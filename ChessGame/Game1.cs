using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using ChessGame.Chess;

namespace ChessGame;

public class Game1 : Game
{
    // ---------------------------------------------------------------
    // Layout constants
    // ---------------------------------------------------------------
    private const int BoardX      = 20;
    private const int BoardY      = 20;
    private const int SquareSize  = 80;
    private const int WindowWidth  = 680;
    private const int WindowHeight = 720;

    private static readonly Color LightSquare    = new Color(240, 217, 181);
    private static readonly Color DarkSquare     = new Color(181, 136,  99);
    private static readonly Color SelectedColor  = new Color(255, 255,   0, 150);
    private static readonly Color LastMoveColor  = new Color(100, 200, 255,  80);
    private static readonly Color ValidMoveColor = new Color(  0, 200,   0, 120);
    private static readonly Color CaptureColor   = new Color(  0, 200,   0,  80);

    // ---------------------------------------------------------------
    // Fields
    // ---------------------------------------------------------------
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private SpriteFont _font;       // size 36 – status bar main line
    private SpriteFont _smallFont;  // size 20 – piece labels & secondary text

    private Texture2D _pixel;             // 1×1 white pixel for rectangles
    private Texture2D _pieceBorderCircle; // radius 37
    private Texture2D _pieceFillCircle;   // radius 33
    private Texture2D _moveIndicator;     // radius 14

    private ChessBoard _board;
    private Position? _selectedPos;
    private List<Move> _legalMoves = new();

    private MouseState _prevMouse;

    // ---------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth  = WindowWidth,
            PreferredBackBufferHeight = WindowHeight
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // ---------------------------------------------------------------
    // Initialize / LoadContent
    // ---------------------------------------------------------------
    protected override void Initialize()
    {
        _board = new ChessBoard();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font      = Content.Load<SpriteFont>("Font");
        _smallFont = Content.Load<SpriteFont>("SmallFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _pieceBorderCircle = CreateCircleTexture(37);
        _pieceFillCircle   = CreateCircleTexture(33);
        _moveIndicator     = CreateCircleTexture(14);
    }

    private Texture2D CreateCircleTexture(int radius)
    {
        int diameter = radius * 2;
        var texture  = new Texture2D(GraphicsDevice, diameter, diameter);
        var data     = new Color[diameter * diameter];
        float r2     = (radius - 0.5f) * (radius - 0.5f);

        for (int y = 0; y < diameter; y++)
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                data[y * diameter + x] = (dx * dx + dy * dy <= r2)
                    ? Color.White
                    : Color.Transparent;
            }

        texture.SetData(data);
        return texture;
    }

    // ---------------------------------------------------------------
    // Update
    // ---------------------------------------------------------------
    protected override void Update(GameTime gameTime)
    {
        var kb    = Keyboard.GetState();
        var mouse = Mouse.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        // Restart
        if (kb.IsKeyDown(Keys.R) && (_board.IsCheckmate || _board.IsStalemate))
        {
            _board = new ChessBoard();
            _selectedPos = null;
            _legalMoves.Clear();
        }

        // Click detection (button-down edge)
        if (mouse.LeftButton == ButtonState.Pressed &&
            _prevMouse.LeftButton == ButtonState.Released)
        {
            HandleClick(mouse.X, mouse.Y);
        }

        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void HandleClick(int px, int py)
    {
        if (_board.IsCheckmate || _board.IsStalemate) return;

        int col = (px - BoardX) / SquareSize;
        int row = (py - BoardY) / SquareSize;

        if (col < 0 || col >= 8 || row < 0 || row >= 8)
        {
            Deselect();
            return;
        }

        var clickPos     = new Position(row, col);
        var clickedPiece = _board.Board[row, col];

        if (_selectedPos.HasValue)
        {
            // Is this a legal move destination?
            Move? validMove = null;
            foreach (var m in _legalMoves)
                if (m.To == clickPos) { validMove = m; break; }

            if (validMove.HasValue)
            {
                _board.MakeMove(validMove.Value);
                Deselect();
            }
            else if (clickedPiece.HasValue && clickedPiece.Value.Color == _board.CurrentTurn)
            {
                // Switch selection to the other friendly piece
                Select(clickPos);
            }
            else
            {
                Deselect();
            }
        }
        else
        {
            if (clickedPiece.HasValue && clickedPiece.Value.Color == _board.CurrentTurn)
                Select(clickPos);
        }
    }

    private void Select(Position pos)
    {
        _selectedPos = pos;
        _legalMoves  = _board.GetLegalMoves(pos);
    }

    private void Deselect()
    {
        _selectedPos = null;
        _legalMoves.Clear();
    }

    // ---------------------------------------------------------------
    // Draw
    // ---------------------------------------------------------------
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin();

        // Build set of valid-move destinations for quick lookup
        var validDests = new HashSet<Position>();
        foreach (var m in _legalMoves)
            validDests.Add(m.To);

        // Draw each square
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                int sx  = BoardX + col * SquareSize;
                int sy  = BoardY + row * SquareSize;
                var rect = new Rectangle(sx, sy, SquareSize, SquareSize);

                // 1. Background
                bool isLight = (row + col) % 2 == 0;
                _spriteBatch.Draw(_pixel, rect, isLight ? LightSquare : DarkSquare);

                // 2. Last-move highlight
                bool isLastMove = (_board.LastMoveFrom.HasValue && pos == _board.LastMoveFrom.Value)
                               || (_board.LastMoveTo.HasValue   && pos == _board.LastMoveTo.Value);
                if (isLastMove)
                    _spriteBatch.Draw(_pixel, rect, LastMoveColor);

                // 3. Selected-piece highlight
                if (_selectedPos.HasValue && pos == _selectedPos.Value)
                    _spriteBatch.Draw(_pixel, rect, SelectedColor);

                // 4. Valid-move indicator
                if (validDests.Contains(pos))
                {
                    if (_board.Board[row, col] == null)
                    {
                        // Empty target – small dot centred on square
                        int ix = sx + SquareSize / 2 - _moveIndicator.Width  / 2;
                        int iy = sy + SquareSize / 2 - _moveIndicator.Height / 2;
                        _spriteBatch.Draw(_moveIndicator, new Vector2(ix, iy), ValidMoveColor);
                    }
                    else
                    {
                        // Capture target – tinted overlay
                        _spriteBatch.Draw(_pixel, rect, CaptureColor);
                    }
                }

                // 5-7. Piece
                var piece = _board.Board[row, col];
                if (piece.HasValue)
                    DrawPiece(sx, sy, piece.Value);
            }
        }

        DrawStatusBar();

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawPiece(int sx, int sy, ChessPiece piece)
    {
        bool isWhite = piece.Color == PieceColor.White;
        int  cx = sx + SquareSize / 2;
        int  cy = sy + SquareSize / 2;

        // 5. Border circle (always black for clear outline)
        DrawCentredTexture(_pieceBorderCircle, cx, cy, Color.Black);

        // 6. Fill circle
        Color fill = isWhite ? Color.White : new Color(55, 55, 55);
        DrawCentredTexture(_pieceFillCircle, cx, cy, fill);

        // 7. Piece label
        string label     = (isWhite ? "W" : "B") + PieceLetter(piece.Type);
        Color  textColor = isWhite ? Color.Black : Color.White;
        var    size      = _smallFont.MeasureString(label);
        var    textPos   = new Vector2(cx - size.X / 2, cy - size.Y / 2);
        _spriteBatch.DrawString(_smallFont, label, textPos, textColor);
    }

    private void DrawCentredTexture(Texture2D tex, int cx, int cy, Color color)
    {
        _spriteBatch.Draw(tex,
            new Vector2(cx - tex.Width / 2, cy - tex.Height / 2),
            color);
    }

    private static string PieceLetter(PieceType type) => type switch
    {
        PieceType.King   => "K",
        PieceType.Queen  => "Q",
        PieceType.Rook   => "R",
        PieceType.Bishop => "B",
        PieceType.Knight => "N",
        PieceType.Pawn   => "P",
        _                => "?"
    };

    private void DrawStatusBar()
    {
        // Status bar occupies y = 660 – 720 (60 px below the 640-px board)
        int line1Y = BoardY + 8 * SquareSize + 8;   // ≈ 668
        int line2Y = line1Y + 32;                    // ≈ 700

        if (_board.IsCheckmate)
        {
            string winner = _board.CurrentTurn == PieceColor.White ? "Black" : "White";
            _spriteBatch.DrawString(_font,      $"CHECKMATE! {winner} wins!", new Vector2(10, line1Y), Color.Red);
            _spriteBatch.DrawString(_smallFont, "Press R to restart",          new Vector2(10, line2Y), Color.White);
        }
        else if (_board.IsStalemate)
        {
            _spriteBatch.DrawString(_font,      "STALEMATE! Draw!",   new Vector2(10, line1Y), Color.Yellow);
            _spriteBatch.DrawString(_smallFont, "Press R to restart", new Vector2(10, line2Y), Color.White);
        }
        else
        {
            string turn = _board.CurrentTurn == PieceColor.White ? "White's Turn" : "Black's Turn";
            _spriteBatch.DrawString(_font, turn, new Vector2(10, line1Y), Color.White);

            if (_board.IsCheck)
            {
                var turnSize = _font.MeasureString(turn);
                _spriteBatch.DrawString(_font, "  CHECK!", new Vector2(10 + turnSize.X, line1Y), Color.Red);
            }
        }
    }
}
