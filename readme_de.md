Links
-----
[![EN](http://beham.biz/chansort/flag_en.png)](https://github.com/PredatH0r/ChanSort/blob/master/readme.md)
[![DE](http://beham.biz/chansort/flag_de.png)](https://github.com/PredatH0r/ChanSort/blob/master/readme_de.md) |
[Download](https://github.com/PredatH0r/ChanSort/releases) | 
[Dokumentation](https://github.com/PredatH0r/ChanSort/wiki/Home-(de)) |
[Forum](https://github.com/PredatH0r/ChanSort/issues) | 
[E-Mail](mailto:horst@beham.biz)

�ber ChanSort
--------------
ChanSort ist eine PC Anwendung, die das Sortieren von Fernsehsenderlisten erlaubt.  
Die meisten modernen Fernseher k�nnen Senderlisten auf einen USB-Stick �bertragen, den man danach am PC anschlie�t.  
ChanSort unterst�tzt diverse Dateiformate von Samsung, LG, Panasonic, Sony, Philips, Hisense, Toshiba, Grundig,
Sharp, Dyon, Blaupunkt, SatcoDX (verwendet von Medion, Nabo, ok., PEAQ, Schaub-Lorenz, Silva-Schneider, Telefunken),
Linux VDR, SAT>IP .m3u und Enigma2 basierender Linux TV-Boxen.

![screenshot](http://beham.biz/chansort/ChanSort-de.png)

Funktionen
--------
- Umreihen von Sendern (direkte Nummerneingabe, auf/ab verschieben, drag&drop, Doppelklick)
- �bernahme der Reihenfolge aus einer Vorlagedatei
- Mehrfachauswahl um mehrere Sender gleichzeitig zu bearbeiten
- Einfache Listenansicht (mit eingereihten Sender zuerst und dahinter alle uneingereihten)
- Nebeneinander-Ansicht von umsortierter und urspr�nglicher Liste (�hnlich wie Playlist und Medienbibliothek)
- Umbenennen und L�schen von Sendern
- Verwalten von Favoriten, Kindersperre, �berspringen und Verstecken von Sendern
- Benutzeroberfl�che in Deutsch, Englisch, Spanisch, T�rkisch, Portugiesisch, Russisch und Rum�nisch
- Unicode-Zeichensatzunterst�tzung f�r Sendernamen (latein, kyrillisch, griechisch, ...)

NICHT unterst�tzt:
- Hinzuf�gen von neuen Transpondern oder Sendern
- �ndern von Tuner-Einstellungen von Sendern (ONID, TSID, SID, Frequenz, APID, VPID, ...)

Manche Funktionen sind nicht bei allen TV-Modellen und Empfangsarten verf�gbar (analog, digital, Sat, Kabel, ...)

! VERWENDUNG AUF EIGENE GEFAHR !
------------------------
Diese Software wurde gro�teils ohne Unterst�tzung durch TV-Hersteller und ohne Zugang zu offiziellen
Unterlagen �ber die Dateiformate erstellt. Es beruht ausschlie�lich auf der Analyse von Dateien, Versuchen and Fehlerkorrekturen.
Es besteht die M�glichkeit von unerwarteten Nebeneffekten oder Schaden am Ger�t (wie in 2 F�llen berichtet).

Hisense ist der einzige Hersteller, der Informationen und ein Testger�t bereitstellten.


Systemvoraussetzungen
-------------------
**Windows**:  
- Windows 7 SP1, Windows 8.1, Windows 10 v1606 or later, Windows 11 (mit x86, x64 oder ARM CPU)
- [Microsoft .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework)  
- Das .NET FW 4.8 funktioniert NICHT unter Windows 7 ohne SP1, Windows 8 oder Windows 10 vor v1606

**Linux**:  
- wine (sudo apt-get install wine)
- winetricks (sudo apt-get install winetricks)
- Starte winetricks, w�hle oder erstelle ein wineprefix (32 bit oder 64 bit), w�hle
  "Installiere Windows DLL oder Komponente", installiere das "dotnet48" Paket and ignore dutzende Popup-Dialoge
- Rechtsklick auf ChanSort.exe, w�hle "�ffnen mit", "Alle Anwendungen", "Eine wine Anwendung"

**Hardware**:  
- USB Stick/SD-Karte zur �bertragung der Senderliste zwischen TV und PC (Ein Stick <= 32 GB mit FAT32-Formatierung
ist DRINGEND empfohlen. (Einige TVs schreiben M�ll auf NTFS bzw. unterst�tzen exFAT gar nicht)


Unterst�tzte TV-Modelle 
---------------------
ChanSort unterst�tzt eine gro�e Anzahl an Dateiformaten, aber es ist unm�glich f�r jede Marke und jedes Modell zu
sagen, welches Format verwendet wird (was sich auch durch Firmware-Updates �ndern kann).  
Diese unvollst�ndige Liste f�hrt einige Beispiele an, die unterst�tzt werden, aber selbst wenn ein Modell oder Marke
hier nicht angef�hrt ist, k�nnte es trotzdem funktiontionieren:
- [Samsung](source/fileformats.md#samsung)
- [LG](source/fileformats.md#lg)
- [Sony](source/fileformats.md#sony)
- [Hisense](source/fileformats.md#hisense)
- [Panasonic](source/fileformats.md#panasonic)
- [Philips](source/fileformats.md#philips)
- [Sharp, Dyon, Blaupunkt, Hisense, Changhong, Grundig, alphatronics, JTC Genesis, ...](source/fileformats.md#sharp)
- [Toshiba](source/fileformats.md#toshiba)
- [Grundig](source/fileformats.md#grundig)
- [SatcoDX: ITT, Medion, Nabo, ok., PEAQ, Schaub-Lorenz, Silva-Schneider, Telefunken, ...](source/fileformats.md#satcodx)
- [VDR](source/fileformats.md#vdr)
- [SAT>IP m3u](source/fileformats.md#m3u)
- [Enigma2](source/fileformats.md#enigma2)

Quellcode selbst �bersetzen
-----------------
Siehe [build.md](source/build.md)

Lizenz (GPLv3)
---------------
GNU General Public Licence, Version 3: http://www.gnu.org/licenses/gpl.html  
Source code is available on https://github.com/PredatH0r/ChanSort

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.

IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
