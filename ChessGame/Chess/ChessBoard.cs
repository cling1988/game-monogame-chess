using System;
using System.Collections.Generic;

namespace ChessGame.Chess
{
    public class ChessBoard
    {
        public ChessPiece?[,] Board { get; private set; }
        public PieceColor CurrentTurn { get; private set; }
        public bool IsCheck { get; private set; }
        public bool IsCheckmate { get; private set; }
        public bool IsStalemate { get; private set; }
        public Position? EnPassantTarget { get; private set; }
        public Position? LastMoveFrom { get; private set; }
        public Position? LastMoveTo { get; private set; }

        public ChessBoard()
        {
            Board = new ChessPiece?[8, 8];
            InitializeBoard();
        }

        // Private copy constructor – does not call InitializeBoard
        private ChessBoard(ChessBoard source)
        {
            Board = (ChessPiece?[,])source.Board.Clone();
            CurrentTurn = source.CurrentTurn;
            EnPassantTarget = source.EnPassantTarget;
            IsCheck = source.IsCheck;
            IsCheckmate = source.IsCheckmate;
            IsStalemate = source.IsStalemate;
            LastMoveFrom = source.LastMoveFrom;
            LastMoveTo = source.LastMoveTo;
        }

        public void InitializeBoard()
        {
            Board = new ChessPiece?[8, 8];
            CurrentTurn = PieceColor.White;
            IsCheck = false;
            IsCheckmate = false;
            IsStalemate = false;
            EnPassantTarget = null;
            LastMoveFrom = null;
            LastMoveTo = null;

            // Black pieces – row 0
            Board[0, 0] = new ChessPiece(PieceType.Rook,   PieceColor.Black);
            Board[0, 1] = new ChessPiece(PieceType.Knight, PieceColor.Black);
            Board[0, 2] = new ChessPiece(PieceType.Bishop, PieceColor.Black);
            Board[0, 3] = new ChessPiece(PieceType.Queen,  PieceColor.Black);
            Board[0, 4] = new ChessPiece(PieceType.King,   PieceColor.Black);
            Board[0, 5] = new ChessPiece(PieceType.Bishop, PieceColor.Black);
            Board[0, 6] = new ChessPiece(PieceType.Knight, PieceColor.Black);
            Board[0, 7] = new ChessPiece(PieceType.Rook,   PieceColor.Black);

            // Black pawns – row 1
            for (int col = 0; col < 8; col++)
                Board[1, col] = new ChessPiece(PieceType.Pawn, PieceColor.Black);

            // White pawns – row 6
            for (int col = 0; col < 8; col++)
                Board[6, col] = new ChessPiece(PieceType.Pawn, PieceColor.White);

            // White pieces – row 7
            Board[7, 0] = new ChessPiece(PieceType.Rook,   PieceColor.White);
            Board[7, 1] = new ChessPiece(PieceType.Knight, PieceColor.White);
            Board[7, 2] = new ChessPiece(PieceType.Bishop, PieceColor.White);
            Board[7, 3] = new ChessPiece(PieceType.Queen,  PieceColor.White);
            Board[7, 4] = new ChessPiece(PieceType.King,   PieceColor.White);
            Board[7, 5] = new ChessPiece(PieceType.Bishop, PieceColor.White);
            Board[7, 6] = new ChessPiece(PieceType.Knight, PieceColor.White);
            Board[7, 7] = new ChessPiece(PieceType.Rook,   PieceColor.White);
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>Returns all fully legal moves for the piece at <paramref name="from"/>.</summary>
        public List<Move> GetLegalMoves(Position from)
        {
            var moves = new List<Move>();
            if (!from.IsValid()) return moves;

            var piece = Board[from.Row, from.Col];
            if (!piece.HasValue || piece.Value.Color != CurrentTurn) return moves;

            foreach (var move in GetPseudoLegalMoves(from, piece.Value))
                if (!MoveLeavesKingInCheck(move))
                    moves.Add(move);

            return moves;
        }

        /// <summary>Applies <paramref name="move"/> and advances the game state.</summary>
        public void MakeMove(Move move)
        {
            LastMoveFrom = move.From;
            LastMoveTo   = move.To;

            // Clear en-passant target; ApplyMove may set a new one.
            EnPassantTarget = null;

            ApplyMove(move);

            CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
            UpdateGameState();
        }

        // ---------------------------------------------------------------
        // Pseudo-legal move generation
        // ---------------------------------------------------------------

        private List<Move> GetPseudoLegalMoves(Position from, ChessPiece piece)
        {
            return piece.Type switch
            {
                PieceType.Pawn   => GetPawnMoves(from, piece),
                PieceType.Knight => GetKnightMoves(from, piece),
                PieceType.Bishop => GetBishopMoves(from, piece),
                PieceType.Rook   => GetRookMoves(from, piece),
                PieceType.Queen  => GetQueenMoves(from, piece),
                PieceType.King   => GetKingMoves(from, piece),
                _                => new List<Move>()
            };
        }

        private List<Move> GetPawnMoves(Position from, ChessPiece piece)
        {
            var moves = new List<Move>();
            int dir          = piece.Color == PieceColor.White ? -1 : 1;
            int startRow     = piece.Color == PieceColor.White ? 6 : 1;
            int promotionRow = piece.Color == PieceColor.White ? 0 : 7;

            // Single step forward
            var oneStep = new Position(from.Row + dir, from.Col);
            if (oneStep.IsValid() && Board[oneStep.Row, oneStep.Col] == null)
            {
                bool isPromo = oneStep.Row == promotionRow;
                moves.Add(new Move(from, oneStep, isPromotion: isPromo));

                // Double step from starting row
                if (from.Row == startRow)
                {
                    var twoStep = new Position(from.Row + dir * 2, from.Col);
                    if (twoStep.IsValid() && Board[twoStep.Row, twoStep.Col] == null)
                        moves.Add(new Move(from, twoStep));
                }
            }

            // Diagonal captures and en passant
            foreach (int dc in new[] { -1, 1 })
            {
                var cap = new Position(from.Row + dir, from.Col + dc);
                if (!cap.IsValid()) continue;

                var target = Board[cap.Row, cap.Col];
                if (target.HasValue && target.Value.Color != piece.Color)
                {
                    bool isPromo = cap.Row == promotionRow;
                    moves.Add(new Move(from, cap, isPromotion: isPromo));
                }

                // En passant
                if (EnPassantTarget.HasValue && cap == EnPassantTarget.Value)
                    moves.Add(new Move(from, cap, isEnPassant: true));
            }

            return moves;
        }

        private List<Move> GetKnightMoves(Position from, ChessPiece piece)
        {
            var moves = new List<Move>();
            int[] drs = { -2, -2, -1, -1,  1,  1,  2,  2 };
            int[] dcs = { -1,  1, -2,  2, -2,  2, -1,  1 };

            for (int i = 0; i < 8; i++)
            {
                var to = new Position(from.Row + drs[i], from.Col + dcs[i]);
                if (!to.IsValid()) continue;
                var tgt = Board[to.Row, to.Col];
                if (tgt == null || tgt.Value.Color != piece.Color)
                    moves.Add(new Move(from, to));
            }
            return moves;
        }

        private List<Move> GetSlidingMoves(Position from, ChessPiece piece, int[] drs, int[] dcs)
        {
            var moves = new List<Move>();
            for (int i = 0; i < drs.Length; i++)
            {
                int r = from.Row + drs[i];
                int c = from.Col + dcs[i];
                while (r >= 0 && r < 8 && c >= 0 && c < 8)
                {
                    var tgt = Board[r, c];
                    if (tgt == null)
                    {
                        moves.Add(new Move(from, new Position(r, c)));
                    }
                    else
                    {
                        if (tgt.Value.Color != piece.Color)
                            moves.Add(new Move(from, new Position(r, c)));
                        break;
                    }
                    r += drs[i];
                    c += dcs[i];
                }
            }
            return moves;
        }

        private List<Move> GetBishopMoves(Position from, ChessPiece piece) =>
            GetSlidingMoves(from, piece, new[] { -1, -1,  1,  1 }, new[] { -1,  1, -1,  1 });

        private List<Move> GetRookMoves(Position from, ChessPiece piece) =>
            GetSlidingMoves(from, piece, new[] { -1,  1,  0,  0 }, new[] {  0,  0, -1,  1 });

        private List<Move> GetQueenMoves(Position from, ChessPiece piece)
        {
            var moves = GetBishopMoves(from, piece);
            moves.AddRange(GetRookMoves(from, piece));
            return moves;
        }

        private List<Move> GetKingMoves(Position from, ChessPiece piece)
        {
            var moves = new List<Move>();
            PieceColor opp = piece.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Normal one-square moves
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0) continue;
                    var to = new Position(from.Row + dr, from.Col + dc);
                    if (!to.IsValid()) continue;
                    var tgt = Board[to.Row, to.Col];
                    if (tgt == null || tgt.Value.Color != piece.Color)
                        moves.Add(new Move(from, to));
                }
            }

            // Castling – only when the king has not moved and is not in check
            if (!piece.HasMoved && !IsInCheck(piece.Color))
            {
                int row = piece.Color == PieceColor.White ? 7 : 0;

                // Temporarily remove the king so it cannot block attacks on transit squares
                Board[from.Row, from.Col] = null;

                // King-side
                var ksRook = Board[row, 7];
                if (ksRook.HasValue && ksRook.Value.Type == PieceType.Rook && !ksRook.Value.HasMoved
                    && Board[row, 5] == null && Board[row, 6] == null
                    && !IsSquareAttacked(new Position(row, 5), opp)
                    && !IsSquareAttacked(new Position(row, 6), opp))
                {
                    moves.Add(new Move(from, new Position(row, 6), isCastling: true));
                }

                // Queen-side
                var qsRook = Board[row, 0];
                if (qsRook.HasValue && qsRook.Value.Type == PieceType.Rook && !qsRook.Value.HasMoved
                    && Board[row, 1] == null && Board[row, 2] == null && Board[row, 3] == null
                    && !IsSquareAttacked(new Position(row, 3), opp)
                    && !IsSquareAttacked(new Position(row, 2), opp))
                {
                    moves.Add(new Move(from, new Position(row, 2), isCastling: true));
                }

                // Restore the king
                Board[from.Row, from.Col] = piece;
            }

            return moves;
        }

        // ---------------------------------------------------------------
        // Move application
        // ---------------------------------------------------------------

        private void ApplyMove(Move move)
        {
            var piece = Board[move.From.Row, move.From.Col];
            if (!piece.HasValue) return;

            var movedPiece = new ChessPiece(piece.Value.Type, piece.Value.Color, hasMoved: true);

            // En passant – remove the captured pawn
            if (move.IsEnPassant)
                Board[move.From.Row, move.To.Col] = null;

            // Castling – move the rook
            if (move.IsCastling)
            {
                int row = move.From.Row;
                if (move.To.Col == 6) // King-side
                {
                    var rook = Board[row, 7]!.Value;
                    Board[row, 5] = new ChessPiece(rook.Type, rook.Color, hasMoved: true);
                    Board[row, 7] = null;
                }
                else // Queen-side
                {
                    var rook = Board[row, 0]!.Value;
                    Board[row, 3] = new ChessPiece(rook.Type, rook.Color, hasMoved: true);
                    Board[row, 0] = null;
                }
            }

            // Double pawn push – record en-passant target
            if (movedPiece.Type == PieceType.Pawn && Math.Abs(move.To.Row - move.From.Row) == 2)
                EnPassantTarget = new Position((move.From.Row + move.To.Row) / 2, move.From.Col);

            // Promotion – auto-promote to queen
            if (move.IsPromotion)
                movedPiece = new ChessPiece(PieceType.Queen, movedPiece.Color, hasMoved: true);

            Board[move.To.Row, move.To.Col]   = movedPiece;
            Board[move.From.Row, move.From.Col] = null;
        }

        // ---------------------------------------------------------------
        // Check / checkmate / stalemate detection
        // ---------------------------------------------------------------

        private bool MoveLeavesKingInCheck(Move move)
        {
            var copy = new ChessBoard(this);
            copy.ApplyMove(move);
            return copy.IsInCheck(CurrentTurn);
        }

        private bool IsInCheck(PieceColor color)
        {
            Position? kingPos = null;
            for (int r = 0; r < 8 && !kingPos.HasValue; r++)
                for (int c = 0; c < 8 && !kingPos.HasValue; c++)
                {
                    var p = Board[r, c];
                    if (p.HasValue && p.Value.Type == PieceType.King && p.Value.Color == color)
                        kingPos = new Position(r, c);
                }

            if (!kingPos.HasValue) return false;

            PieceColor opp = color == PieceColor.White ? PieceColor.Black : PieceColor.White;
            return IsSquareAttacked(kingPos.Value, opp);
        }

        public bool IsSquareAttacked(Position pos, PieceColor attackingColor)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var p = Board[r, c];
                    if (!p.HasValue || p.Value.Color != attackingColor) continue;
                    if (CanAttackSquare(new Position(r, c), p.Value, pos))
                        return true;
                }
            return false;
        }

        private bool CanAttackSquare(Position from, ChessPiece piece, Position target)
        {
            int dr = target.Row - from.Row;
            int dc = target.Col - from.Col;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    int dir = piece.Color == PieceColor.White ? -1 : 1;
                    return dr == dir && (dc == -1 || dc == 1);

                case PieceType.Knight:
                    return (Math.Abs(dr) == 2 && Math.Abs(dc) == 1)
                        || (Math.Abs(dr) == 1 && Math.Abs(dc) == 2);

                case PieceType.Bishop:
                    if (Math.Abs(dr) != Math.Abs(dc) || dr == 0) return false;
                    return IsPathClear(from, target);

                case PieceType.Rook:
                    if (dr != 0 && dc != 0) return false;
                    if (dr == 0 && dc == 0) return false;
                    return IsPathClear(from, target);

                case PieceType.Queen:
                    if (dr == 0 && dc == 0) return false;
                    if (dr == 0 || dc == 0) return IsPathClear(from, target);
                    if (Math.Abs(dr) == Math.Abs(dc)) return IsPathClear(from, target);
                    return false;

                case PieceType.King:
                    return Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1 && (dr != 0 || dc != 0);

                default:
                    return false;
            }
        }

        private bool IsPathClear(Position from, Position to)
        {
            int dr = Math.Sign(to.Row - from.Row);
            int dc = Math.Sign(to.Col - from.Col);
            int r  = from.Row + dr;
            int c  = from.Col + dc;
            while (r != to.Row || c != to.Col)
            {
                if (Board[r, c] != null) return false;
                r += dr;
                c += dc;
            }
            return true;
        }

        private void UpdateGameState()
        {
            IsCheck = IsInCheck(CurrentTurn);

            bool hasLegalMoves = false;
            for (int r = 0; r < 8 && !hasLegalMoves; r++)
                for (int c = 0; c < 8 && !hasLegalMoves; c++)
                {
                    var p = Board[r, c];
                    if (p.HasValue && p.Value.Color == CurrentTurn)
                        if (GetLegalMoves(new Position(r, c)).Count > 0)
                            hasLegalMoves = true;
                }

            if (!hasLegalMoves)
            {
                if (IsCheck)
                    IsCheckmate = true;
                else
                    IsStalemate = true;
            }
        }
    }
}
