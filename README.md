# game-monogame-chess

A fully playable two-player chess game built with the [MonoGame](https://www.monogame.net/) cross-platform desktop framework.

---

## Screenshot

```
┌─────────────────────────────────────────┐
│  ♜ ♞ ♝ ♛ ♚ ♝ ♞ ♜  (Black back rank)   │
│  ♟ ♟ ♟ ♟ ♟ ♟ ♟ ♟  (Black pawns)       │
│  ·  ·  ·  ·  ·  ·  ·  ·               │
│  ·  ·  ·  ·  ·  ·  ·  ·               │
│  ·  ·  ·  ·  ·  ·  ·  ·               │
│  ·  ·  ·  ·  ·  ·  ·  ·               │
│  ♙ ♙ ♙ ♙ ♙ ♙ ♙ ♙  (White pawns)       │
│  ♖ ♘ ♗ ♕ ♔ ♗ ♘ ♖  (White back rank)   │
│                                         │
│  White's Turn                           │
└─────────────────────────────────────────┘
```

---

## Features

- **All chess rules** implemented:
  - Legal moves for all 6 piece types (King, Queen, Rook, Bishop, Knight, Pawn)
  - Check detection — illegal moves that leave your king in check are blocked
  - Checkmate and stalemate detection
  - En passant captures
  - Castling (king-side and queen-side)
  - Pawn promotion (auto-promotes to Queen)
- **Two-player local gameplay** — White always moves first
- **Visual highlights**:
  - Selected piece highlighted in yellow
  - Valid move destinations shown as green dots (empty squares) or green tint (captures)
  - Last move highlighted in light blue
- **Status bar** showing current turn, `CHECK!` alert in red, and game-over messages
- **Restart** at any time after checkmate or stalemate with the `R` key

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- A desktop OS: **Windows**, **macOS**, or **Linux**

> **Linux users:** you may need the following native libraries:
> ```bash
> sudo apt-get install libsdl2-dev libopenal-dev
> ```

---

## Building & Running

```bash
# Clone the repository
git clone https://github.com/cling1988/game-monogame-chess.git
cd game-monogame-chess

# Build
dotnet build ChessGame/ChessGame.csproj

# Run
dotnet run --project ChessGame/ChessGame.csproj
```

Or open the solution in Visual Studio / Rider and press **F5**.

---

## How to Play

| Action | Control |
|--------|---------|
| Select a piece | Left-click on a piece belonging to the current player |
| Move a piece | Left-click on a highlighted destination square |
| Switch selection | Left-click on another friendly piece |
| Deselect | Left-click outside valid squares |
| Restart (after game over) | Press **R** |
| Quit | Press **Escape** |

---

## Project Structure

```
ChessGame/
├── Chess/
│   ├── ChessTypes.cs      # Core types: PieceType, PieceColor, Position, ChessPiece, Move
│   └── ChessBoard.cs      # Game logic: move generation, check/checkmate/stalemate, castling, en passant
├── Content/
│   ├── Font.spritefont    # 36pt font for status bar
│   ├── SmallFont.spritefont # 20pt font for piece labels
│   └── Content.mgcb       # MonoGame content build pipeline config
├── Game1.cs               # MonoGame game loop: rendering and mouse input
├── Program.cs             # Entry point
└── ChessGame.csproj       # Project file (targets net9.0, MonoGame.Framework.DesktopGL 3.8)
```

---

## Technology

- **Framework:** [MonoGame 3.8](https://www.monogame.net/) — `MonoGame.Framework.DesktopGL`
- **Language:** C# 13 / .NET 9
- **Graphics:** All visuals drawn programmatically with `Texture2D` and `SpriteBatch` — no external image assets required
