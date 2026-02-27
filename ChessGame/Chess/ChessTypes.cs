using System;

namespace ChessGame.Chess
{
    public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }
    public enum PieceColor { White, Black }

    public struct Position : IEquatable<Position>
    {
        public int Row;
        public int Col;

        public Position(int row, int col) { Row = row; Col = col; }

        public bool IsValid() => Row >= 0 && Row < 8 && Col >= 0 && Col < 8;

        public bool Equals(Position other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Position p && Equals(p);
        public override int GetHashCode() => Row * 8 + Col;

        public static bool operator ==(Position a, Position b) => a.Equals(b);
        public static bool operator !=(Position a, Position b) => !a.Equals(b);
    }

    public struct ChessPiece
    {
        public PieceType Type;
        public PieceColor Color;
        public bool HasMoved;

        public ChessPiece(PieceType type, PieceColor color, bool hasMoved = false)
        {
            Type = type;
            Color = color;
            HasMoved = hasMoved;
        }
    }

    public struct Move
    {
        public Position From;
        public Position To;
        public bool IsEnPassant;
        public bool IsCastling;
        public bool IsPromotion;

        public Move(Position from, Position to,
                    bool isEnPassant = false,
                    bool isCastling = false,
                    bool isPromotion = false)
        {
            From = from;
            To = to;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
            IsPromotion = isPromotion;
        }
    }
}
