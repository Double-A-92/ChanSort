﻿using System.Xml;
using ChanSort.Api;

namespace ChanSort.Loader.Philips
{
  internal class Channel : ChannelInfo
  {
    public Channel(SignalSource source, long index, int oldProgNr, string name) : base(source, index, oldProgNr, name)
    {
    }

    internal Channel(SignalSource source, int order, int rowId, XmlNode setupNode)
    {
      this.SignalSource = source;
      this.RecordOrder = order;
      this.RecordIndex = rowId;
      this.SetupNode = setupNode;
    }

    /// <summary>
    /// index of the record in the AntennaPresetTable / CablePresetTable file for the channel, matched by (onid + tsid + sid)
    /// </summary>
    public int PresetTableIndex { get; set; } = -1;

    // fields relevant for ChannelMap_100 and later (XML nodes)
    public readonly XmlNode SetupNode;
    public string RawName;
    public string RawSatellite;
    public int Format;

  }
}
