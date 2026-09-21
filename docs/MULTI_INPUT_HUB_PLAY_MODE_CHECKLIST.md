# Multi-input Hub Play Mode checklist

1. Open `Assets/_Project/Scenes/Prototype.unity` and enter Play Mode.
2. Place one Extractor on each of the two Circle Rune Stone deposits.
3. Build two independent production lines and route their final belts into two different sides of the Hub. Do not join the belt lines.
4. Confirm both incoming runes disappear from their final belts and each matching requirement in **Shared Throughput** advances once per delivered rune.
5. Route Acceleration Runes through both inputs during **Sustained Acceleration**. Confirm the displayed delivery rate includes both lines and a short buffer dump does not complete the sustained window early.
6. Confirm each delivered Acceleration Rune increments **Hub Acceleration Runes**, then install one in an Engraver and confirm inventory decreases by exactly one while the Engraver throughput increases.
7. Leave a non-Hub processing machine fed from two sides and confirm only its configured input side transfers, preserving the existing single-input machine behavior.
