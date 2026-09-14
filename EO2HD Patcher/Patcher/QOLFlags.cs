namespace EO2HD_Patcher.Patcher;

public enum QOLFlags
{
    FOEs_Give_Experience,
    Conditionals_Always_Drop,
    Exp_Multiplier
}

/*

EXP is a pain to get right. 

https://w.atwiki.jp/sekaiju_maze2/pages/25.html#id_e08b04e1
This table is the EXP you need each level to advance to the next.
It is simply quadratic,  with each entry needing an additional 100+(2 curLevel)
to advance to the next level.
This accumulates to be 1,391,500 EXP to reach lvl 70 in 2.


What is the EXP of a given formation per floor? Do bosses exceed that by a shared margin?
Unfortunately, I'm not yet versed in processing tile data, so I don't know how frequent formations are per floor.
So, we can compare each individual enemy to look at how much EXP they're worth per level, and use that as a basis.

However, when we do that, we can see that it doesn't really line up.

So, my method for FOEs was to take an enemy with comparable stats attack (STR/TEC) and defense (VIT), since that would imply
an enemy with roughly the same ferocity.
Then, to normalize the EXP value to the expected difficulty to kill (since FOEs have much higher HP), I took a ratio of the
compared creature's HP and the FOEs, and multiplied the compared creature's EXP gain by said ratio.
This worked...reasonably well? FOEs give a good chunk more EXP than normal enemies, but not nearly the amount of a boss.
The comparison starts getting worse as you go up in strata, though, as not many enemies have the statlines of a 5th or 6th
stratum FOE (the 6th stratum FOEs almost all get compared to Sauromars), but it still works out well enough.

As for bosses, it wasn't too difficult to come up with the calculus of a comparative EXP curve. It has one naive assumption,
and the assumed f'' becomes linear when manually extrapolated, but other than that the values line up pretty well.


EO2U comparison:

I can't find Fafnir Knight's table, but I can find Untold 1's.
https://w.atwiki.jp/shinsekaiju_maze/pages/38.html#id_6bf65dd0

...Which shows fairly clearly that a comparison between the two is not viable,
especially if we look at the EXP drops of the strata bosses:

Chimaera:    21,000 in 2 (plus 0 from adds) |   4,000 in U (plus 2,000 from adds)
Hellion:     54,000 in 2 |  20,000 in U (plus 1,000 from adds)
Scylla:     100,000 in 2 |  52,000 in U
Harpuia:    150,000 in 2 | 100,000 in U
Juggernaut: 200,000 in 2 | 145,000 in U
Overlord:      0(?) in 2 | 350,000 in U(?)
 
*/