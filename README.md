# JaiSeqX

Successor of BMSXPX! 

JaiSeqX is a wrapper for the JAudio / BMS engine for the Nintendo Gamecube. It enables emulated environments to read JAudio files for playback.

It currently supports sequences from the following games:
  * Luigis Mansion (90%)
  * Super Mario Sunshine (80%)
  * Mario Kart: Double Dash (10%, work in progress) -- Sequences will play but are hilariously out of tune. 
  * Legend of Zelda: Windwaker (80%)
  * Super Mario Galaxy: (1%, work in progress) -- Needs to load JaiChord.arc ,  finish BAA instrument format. 
  * Pikmin 1 & 2 (70%)
  * Others? ( 0% , unknown ) 
  
  
**ADSR is currently not implemented.**
**Pitch bend is currently not implemented.**
  
# Visualizer 
JaiSeqX Comes with a visualizer to watch what the sequences are doing. Video Below:
  
[![fuf](https://img.youtube.com/vi/f1tRAnuDKww/0.jpg)](https://www.youtube.com/watch?v=f1tRAnuDKww)
  
The visualizer in the current version has different commands. For example, you can mute channels by using the alphabet, where A = channel 0. Each key will toggle a mute, so.
 
a = 0 
b = 1 
c = 2 
d = 3 
e = 4 .... and so on.

You can also modify the tempo with the arrow keys. So if a song is too slow, you can use the UP and DOWN arrow keys to adjust the playback ppqn, and the LEFT and RIGHT arrow keys to adjust the BPM.  This information will be displayed at the top (Most recent version, not featured in the video) 

 
