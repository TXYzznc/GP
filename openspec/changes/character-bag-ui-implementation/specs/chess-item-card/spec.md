## ADDED Requirements

### Requirement: Chess item card displays chess information
The ChessItemUI_Small card SHALL display chess's portrait, name, level/stage, and quality indicator.

#### Scenario: Card displays basic chess info
- **WHEN** ChessItemUI_Small is instantiated for a chess
- **THEN** it shows chess portrait image, name, current stage/level, and quality badge

#### Scenario: Card supports click selection
- **WHEN** user clicks a ChessItemUI_Small card
- **THEN** the card is selected and parent CharacterBagUI updates all detail panels

#### Scenario: Card supports hover effects
- **WHEN** user hovers over a ChessItemUI_Small card
- **THEN** card shows hover visual feedback (glow, scale, or brightness change)

### Requirement: Quality indicator shows chess rarity
The card SHALL display a visual quality indicator based on chess's quality level (1-4: blue, purple, gold, rainbow).

#### Scenario: Quality badge colors by rarity
- **WHEN** card is rendered for quality 1/2/3/4
- **THEN** quality indicator shows blue/purple/gold/rainbow color respectively
