﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ChanSort.Api;
using Microsoft.Data.Sqlite;

namespace ChanSort.Loader.Panasonic;

/*
 * Serializer for the 2022 Android based Panasonic LS 500, LX 700, ... series file format
 * The format uses a directory tree with
 * /hotel.bin (irrelevant content)
 * /mnt/vendor/tvdata/database/tv.db Sqlite database (probably for the menu, EPG, ...)
 * /mnt/vendor/tvdata/database/channel/idtvChannel.bin (probably for the tuner)
 */
internal class IdtvChannelSerializer : SerializerBase
{
  #region idtvChannel.bin file format

  /*
   The idtvChannel.bin seems to be related to the TV's DVB tuner. 
   It does not contain some streaming related channels that can be found in tv.db, but contains lots of DVB channels that are not includedin tv.bin (probably filtered out there by country settings)
   When changing program numbers through the TV's menu, the data records in the .bin file get physically reordered to match the logical order.
  */

  [Flags]
  enum Flags : ushort
  {
    Encrypted = 0x0002,
    IsFavorite = 0x0080,
    Deleted = 0x0100,
    Hidden = 0x0400
  }

  [StructLayout(LayoutKind.Sequential)]
  unsafe struct IdtvChannel
  {
    public short U0; // always 1
    public short RecordLength; // 60 + length of channel name
    public short U4; // always 6
    public fixed byte U6[10]; // all 00
    public uint Freq; // Hz for DVB-C/T, kHz for DVB-S
    public uint SymRate; // in Sym/s, like 22000000
    public short U24; // always 100
    public short U26; // always 0
    public short U28; // always 0
    public short ProgNr;
    public fixed byte U32[4]; // e.g. 0a 01 00 00
    public Flags Flags;
    public fixed byte U38[4]; // 12 07 01 02
    public short Tsid;
    public short Onid;
    public short Sid;
    public fixed byte U48[16];
    //public fixed byte ChannelName[RecordLength - 60]; // pseudo-C# description of variable length channel name UTF8 data at end of structure
  }

  #endregion

  class ChannelDictEntry
  {
    public ChannelInfo Channel;
    public long FilePosition;
  }

  private readonly string dbFile;
  private readonly string binFile;
  private readonly Dictionary<long, ChannelDictEntry> channelDict = new();

  private readonly StringBuilder log = new();

  #region ctor()
  public IdtvChannelSerializer(string inputFile) : base(inputFile)
  {
    dbFile = inputFile;
    binFile = Path.Combine(Path.GetDirectoryName(dbFile), "channel", "idtvChannel.bin");

    this.Features.CanSaveAs = false;
    this.Features.FavoritesMode = FavoritesMode.Flags;

    this.DataRoot.AddChannelList(new ChannelList(SignalSource.Antenna | SignalSource.MaskTvRadioData, "Antenna"));
    this.DataRoot.AddChannelList(new ChannelList(SignalSource.Cable | SignalSource.MaskTvRadioData, "Cable"));
    this.DataRoot.AddChannelList(new ChannelList(SignalSource.Sat | SignalSource.MaskTvRadioData, "Sat"));
    foreach (var list in this.DataRoot.ChannelLists)
    {
      var names = list.VisibleColumnFieldNames;
      names.Remove(nameof(ChannelInfo.ShortName));
      names.Remove(nameof(ChannelInfo.Satellite));
      names.Remove(nameof(ChannelInfo.PcrPid));
      names.Remove(nameof(ChannelInfo.VideoPid));
      names.Remove(nameof(ChannelInfo.AudioPid));
      names.Remove(nameof(ChannelInfo.Provider));
      names.Add(nameof(ChannelInfo.Debug));
    }
  }
  #endregion

  #region Load()
  public override void Load()
  {
    if (!File.Exists(dbFile))
      throw new FileLoadException("expected file not found: " + dbFile);
    if (!File.Exists(binFile))
      throw new FileLoadException("expected file not found: " + binFile);

    string connString = "Data Source=" + this.dbFile;
    using var db = new SqliteConnection(connString);
    db.Open();
    using var cmd = db.CreateCommand();

    try
    {
      cmd.CommandText = "SELECT count(1) FROM sqlite_master WHERE type = 'table' and name in ('android_metadata', 'channels')";
      var result = Convert.ToInt32(cmd.ExecuteScalar()); // if the database file is corrupted, the execption will be thrown here and not when opening it
      if (result != 2)
        throw new FileLoadException("File doesn't contain the expected android_metadata/channels tables");
    }
    catch (SqliteException)
    {
      // when the USB stick is removed without going through the menu and "safely remove USB", the DB is often corrupted and can't be opened
      View.Default.MessageBox(
        "The Panasonic tv.db file in this channel list is corrupted and can't be loaded.\n\n"+
        "This very often happens when the USB stick is unplugged from the TV without using the TV menu to \"safely remove USB device\" before.\n" +
        "Please export the list again and use this menu before unplugging the stick from the TV.");
      //throw new FileLoadException("Corrupt database file");
      return;
    }

    this.ReadChannelsFromDatabase(cmd);
    this.ReadIdtvChannelsBin();
  }
  #endregion

  #region ReadChannelsFromDatabase()
  private void ReadChannelsFromDatabase(SqliteCommand cmd)
  {
    cmd.CommandText = "select * from channels where type in ('TYPE_DVB_S','TYPE_DVB_C','TYPE_DVB_T','TYPE_DVB_T2')";
    using var r = cmd.ExecuteReader();
      
    var cols = new Dictionary<string, int>();
    for (int i = 0, c = r.FieldCount; i < c; i++)
      cols[r.GetName(i)] = i;

    while (r.Read())
    {
      var id = r.GetInt64(cols["_id"]);
      var type = r.GetString(cols["type"]);
      var svcType = r.GetString(cols["service_type"]);
      var name = r.IsDBNull(cols["display_name"]) ? "" : r.GetString(cols["display_name"]);
      var progNrStr = r.GetString(cols["display_number"]);
      if (!int.TryParse(progNrStr, out var progNr))
        continue;

      SignalSource signalSource = 0;
      switch (type)
      {
        case "TYPE_DVB_C": signalSource |= SignalSource.Cable; break;
        case "TYPE_DVB_S": signalSource |= SignalSource.Sat; break;
        case "TYPE_DVB_T": signalSource |= SignalSource.Antenna; break;
        case "TYPE_DVB_T2": signalSource |= SignalSource.Antenna; break;
      }

      switch (svcType)
      {
        case "SERVICE_TYPE_AUDIO": signalSource |= SignalSource.Radio; break;
        case "SERVICE_TYPE_AUDIO_VIDEO": signalSource |= SignalSource.Tv; break;
        default: signalSource |= SignalSource.Data; break;
      }

      var ch = new ChannelInfo(signalSource, id, progNr, name);
      ch.Lock = r.GetBoolean(cols["locked"]);
      ch.Skip = !r.GetBoolean(cols["browsable"]);
      ch.Hidden = !r.GetBoolean(cols["searchable"]);
      ch.Encrypted = r.GetBoolean(cols["scrambled"]);

      ch.OriginalNetworkId = r.GetInt16(cols["original_network_id"]);
      ch.TransportStreamId = r.GetInt16(cols["transport_stream_id"]);
      ch.ServiceId = r.GetInt32(cols["service_id"]);
      ch.FreqInMhz = r.GetInt64(cols["internal_provider_flag1"]) / 1000; // for DVB-S it is in MHz, for DVB-C/T it is in kHz
      if (ch.FreqInMhz >= 13000)
        ch.FreqInMhz /= 1000;
      ch.SymbolRate = r.GetInt32(cols["internal_provider_flag4"]) / 1000;
      if ((signalSource & SignalSource.Radio) != 0)
        ch.ServiceTypeName = "Radio";
      else if ((signalSource & SignalSource.Tv) != 0)
        ch.ServiceTypeName = r.GetBoolean(cols["is_hd"]) ? "HD-TV" : "SD-TV";
      else
        ch.ServiceTypeName = "Data";
      ch.RecordOrder = r.GetInt32(cols["channel_index"]); // record index in the idtvChannel.bin file
      ch.Favorites = (Favorites)r.GetByte(cols["favorite"]);

      var list = this.DataRoot.GetChannelList(signalSource);
      this.DataRoot.AddChannel(list, ch);

      channelDict.Add(ch.RecordOrder, new ChannelDictEntry() { Channel = ch });
    }
  }
  #endregion

  #region ReadIdtvChannelsBin()
  private void ReadIdtvChannelsBin()
  {
    // verify MD5 checksum
    var data = File.ReadAllBytes(this.binFile);
    var md5 = MD5.Create();
    var hash = md5.ComputeHash(data, 24, data.Length - 24);
    int i;
    for (i = 0; i < 16; i++)
    {
      if (data[8 + i] != hash[i])
        throw new FileLoadException("Invalid MD5 checksum in " + binFile);
    }


    using var strm = new MemoryStream(data);
    using var r = new BinaryReader(strm);

    r.ReadBytes(2 + 2); // 00 00, 4b 09
    var numRecords = r.ReadUInt16();
    r.ReadBytes(2); // 00 00
    r.ReadBytes(16); // md5
    i = 0;

    var structSize = Marshal.SizeOf<IdtvChannel>();
    while (strm.Position + structSize <= data.Length)
    {
      var off = strm.Position;
        
      // C# trickery to read binary data into a structure
      var bytes = r.ReadBytes(structSize);
      GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
      var chan = Marshal.PtrToStructure<IdtvChannel>(handle.AddrOfPinnedObject());
      handle.Free();

      var freq = chan.Freq / 1000;
      if (freq >= 13000)
        freq /= 1000;
      var symRate = chan.SymRate / 1000;
      var progNr = chan.ProgNr;
      var name = Encoding.UTF8.GetString(r.ReadBytes(chan.RecordLength - 60));

      log.AppendLine($"{i}\t{name}\t{progNr}\t{chan.Onid}-{chan.Tsid}-{chan.Sid}\t{(ushort)chan.Flags:X4}");

      if (channelDict.TryGetValue(i, out var entry))
      {
        entry.FilePosition = off;

        var ch = entry.Channel;
        if (ch.OldProgramNr != progNr)
          throw new FileLoadException($"mismatching program_number between tv.db _id {ch.RecordIndex} ({ch.OldProgramNr}) and idtvChannel.bin record {i} ({progNr})");
        if (ch.Name != name)
          throw new FileLoadException($"mismatching name between tv.db _id {ch.RecordIndex} ({ch.Name}) and idtvChannel.bin record {i} ({name})");
        if (Math.Abs(ch.FreqInMhz - freq) > 2)
          throw new FileLoadException($"mismatching frequency between tv.db _id {ch.RecordIndex} ({ch.FreqInMhz}) and idtvChannel.bin record {i} ({freq})");
        if (Math.Abs(ch.SymbolRate - symRate) > 2)
          throw new FileLoadException($"mismatching symbol rate between tv.db _id {ch.RecordIndex} ({ch.SymbolRate}) and idtvChannel.bin record {i} ({symRate})");

        if (ch.Encrypted != ((chan.Flags & Flags.Encrypted) != 0))
          throw new FileLoadException($"mismatching crypt-flag between tv.db _id {ch.RecordIndex} ({ch.Encrypted}) and idtvChannel.bin record {i}");
        if (ch.Hidden != ((chan.Flags & Flags.Hidden) != 0))
          throw new FileLoadException($"mismatching hide-flag between tv.db _id {ch.RecordIndex} ({ch.Hidden}) and idtvChannel.bin record {i}");
        if ((ch.Favorites == 0) != ((chan.Flags & Flags.IsFavorite) == 0))
          throw new FileLoadException($"mismatching favorites-info between tv.db _id {ch.RecordIndex} ({ch.Favorites}) and idtvChannel.bin record {i}");

        ch.AddDebug((ushort)chan.Flags);
      }

      ++i;
    }

    if (i < numRecords)
      throw new FileLoadException($"idtvChannel contains only {i} data records, but expected {numRecords}");

    // make sure no channel from tv.db refers to a record_index that does not exist in idtvChannel.bin
    foreach (var list in this.DataRoot.ChannelLists)
    {
      foreach (var ch in list.Channels)
      {
        if (ch.RecordOrder < 0 || ch.RecordOrder >= numRecords)
          throw new FileLoadException($"{list.ShortCaption} channel with _id {ch.RecordIndex} refers to non-existing index {ch.RecordOrder} in idtvChannel.bin");
      }
    }
  }
  #endregion

  #region GetDataFilePaths()
  public override IEnumerable<string> GetDataFilePaths()
  {
    // return the list of files where ChanSort will create a .bak copy
    return new[] { dbFile, binFile };
  }
  #endregion

  #region Save()
  public override void Save(string tvOutputFile)
  {
    string connString = "Data Source=" + this.dbFile;
    using var db = new SqliteConnection(connString);
    db.Open();

    var data = File.ReadAllBytes(binFile);
    var w = new BinaryWriter(new MemoryStream(data));

    using var trans = db.BeginTransaction();
      
    using var upd = db.CreateCommand();
    upd.CommandText = "update channels set display_number=@progNr, browsable=@browseable, searchable=@searchable, locked=@locked, favorite=@fav where _id=@id";
    upd.Parameters.Add("@id", SqliteType.Integer);
    upd.Parameters.Add("@progNr", SqliteType.Text);
    upd.Parameters.Add("@browseable", SqliteType.Integer);
    upd.Parameters.Add("@searchable", SqliteType.Integer);
    upd.Parameters.Add("@locked", SqliteType.Integer);
    upd.Parameters.Add("@fav", SqliteType.Integer);
    upd.Prepare();

    using var del = db.CreateCommand();
    del.CommandText = "delete from channels where _id=@id";
    del.Parameters.Add("@id", SqliteType.Integer);
    del.Prepare();

    var offProgNr = (int)Marshal.OffsetOf<IdtvChannel>(nameof(IdtvChannel.ProgNr));
    var offFlags = (int)Marshal.OffsetOf<IdtvChannel>(nameof(IdtvChannel.Flags));
    foreach (var list in this.DataRoot.ChannelLists)
    {
      foreach (var ch in list.Channels)
      {
        if (ch.IsProxy)
          continue;
        if (ch.NewProgramNr < 0 || ch.IsDeleted)
        {
          del.Parameters["@id"].Value = ch.RecordIndex;
          del.ExecuteNonQuery();
        }
        else
        {
          upd.Parameters["@id"].Value = ch.RecordIndex;
          upd.Parameters["@progNr"].Value = ch.NewProgramNr;
          upd.Parameters["@browseable"].Value = !ch.Skip;
          upd.Parameters["@searchable"].Value = !ch.Hidden;
          upd.Parameters["@locked"].Value = ch.Lock;
          upd.Parameters["@fav"].Value = (int)ch.Favorites;
          upd.ExecuteNonQuery();

          var entry = channelDict[ch.RecordOrder];
          w.Seek((int)entry.FilePosition + offProgNr, SeekOrigin.Begin);
          w.Write((ushort)ch.NewProgramNr);

          // update flags
          var off = (int)entry.FilePosition + offFlags;
          var flags = BitConverter.ToUInt16(data, off);
          if (ch.Favorites == 0)
            flags = (ushort)(flags & ~(ushort)Flags.IsFavorite);
          else
            flags = (ushort)(flags | (ushort)Flags.IsFavorite);
          if (ch.Hidden)
            flags = (ushort)(flags | (ushort)Flags.Hidden);
          else
            flags = (ushort)(flags & ~(ushort)Flags.Hidden);
          w.Seek((int)entry.FilePosition + offFlags, SeekOrigin.Begin);
          w.Write(flags);
        }
      }
    }
    trans.Commit();

    w.Flush();

    // TODO reorder data records in .bin file based on progNr
    // this also requires to update all the FilePositions in this.channelMap

    // update MD5 checksum
    var md5 = MD5.Create();
    var checksum = md5.ComputeHash(data, 8 + 16, data.Length - 8 - 16);
    Array.Copy(checksum, 0, data, 8, 16);

    File.WriteAllBytes(binFile, data);
  }
  #endregion

  public override string GetFileInformation()
  {
    return base.GetFileInformation() + "\n\n\n" + this.log;
  }
}