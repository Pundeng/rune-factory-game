# Rune visual Play Mode check

The Belt prefab uses `Images/RuneVisualPalette.asset`, which holds the sigil scale, outline width, and outline color. Both neutral and Fire sigils use `Images/RuneSigilFire.mat`; the renderer selects the Fire gradient for infused runes. If the palette assignment is missing, run `Fantasy Shapez > Set Up Rune Visuals` before entering Play Mode.

1. In `Prototype`, place an Extractor, an Engraver, an Element Infuser, and connected Belts to the Hub. Select Spirit on the Engraver and Fire on the Infuser.
2. Confirm the Extractor's empty Circle has only the neutral stone. After engraving, confirm the larger Spirit sigil has a dark outline and no pink material. After infusion, confirm the same sigil receives an orange-to-yellow Fire gradient while the stone stays neutral.
3. Select Acceleration on another Engraver and confirm its larger speed sigil has the same readable outline and no pink material. Check transparency and alignment while runes move around a belt turn and while waiting at a blocked output.
4. Confirm the Hub continues to consume delivered runes and objective progress is unchanged. Check the Console for shader or missing-reference errors.
