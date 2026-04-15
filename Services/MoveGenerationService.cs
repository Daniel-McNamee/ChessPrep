using ChessProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;

namespace ChessProject.Services
{
    public class MoveGenerationService
    {
        // Main method to get legal moves for a piece on a given square
        public List<SquareViewModel> GetMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares,
            SquareViewModel enPassantTarget = null)
        {
            // List to hold potential moves
            var moves = new List<SquareViewModel>(); 

            if (from == null || !from.HasPiece)
                return moves;

            // Generate moves based on piece type
            switch (from.Piece.Type)
            {
                case PieceType.Pawn:
                    moves.AddRange(GetPawnMoves(from, squares, enPassantTarget));
                    break;

                case PieceType.Knight:
                    moves.AddRange(GetKnightMoves(from, squares));
                    break;

                case PieceType.Bishop:
                    moves.AddRange(GetBishopMoves(from, squares));
                    break;

                case PieceType.Rook:
                    moves.AddRange(GetRookMoves(from, squares));
                    break;

                case PieceType.Queen:
                    moves.AddRange(GetQueenMoves(from, squares));
                    break;

                case PieceType.King:
                    moves.AddRange(GetKingMoves(from, squares));
                    break;
            }

            return moves;
        }

        // Main method to filter moves to only legal ones (not leaving king in check)
        public List<SquareViewModel> GetLegalMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares,
            SquareViewModel enPassantTarget)
        {
            var pseudoMoves = GetMoves(from, squares, enPassantTarget);

            var legalMoves = new List<SquareViewModel>();

            var colour = from.Piece.Colour;

            foreach (var move in pseudoMoves)
            {
                var (captured, f, t) = MakeTemporaryMove(from, move);

                bool isInCheck = IsKingInCheck(squares, colour);

                UndoTemporaryMove(f, t, captured);

                if (!isInCheck)
                    legalMoves.Add(move);
            }

            return legalMoves;
        }

        // Helper to find a square by row and column
        private SquareViewModel GetSquare(IEnumerable<SquareViewModel> squares, int row, int col)
        {
            return squares.FirstOrDefault(s => s.Row == row && s.Column == col);
        }

        #region Move Generation Methods
        // Knight Moves
        private List<SquareViewModel> GetKnightMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            int[,] offsets = new int[,]
            {
                { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 },
                { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int newRow = from.Row + offsets[i, 0];
                int newCol = from.Column + offsets[i, 1];

                if (newRow < 0 || newRow > 7 || newCol < 0 || newCol > 7)
                    continue;

                var target = GetSquare(squares, newRow, newCol);

                if (target == null)
                    continue;

                // Cannot capture own piece
                if (target.HasPiece &&
                    target.Piece.Colour == from.Piece.Colour)
                    continue;

                moves.Add(target);
            }

            return moves;
        }

        // Pawn Moves 
        private List<SquareViewModel> GetPawnMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares,
            SquareViewModel enPassantTarget)
        {
            var moves = new List<SquareViewModel>();

            int direction = from.Piece.Colour == PieceColour.White ? -1 : 1;

            int startRow = from.Piece.Colour == PieceColour.White ? 6 : 1;

            int forwardRow = from.Row + direction;

            // One square forward
            if (forwardRow >= 0 && forwardRow <= 7)
            {
                var forwardSquare = GetSquare(squares, forwardRow, from.Column);

                if (forwardSquare != null && !forwardSquare.HasPiece)
                {
                    moves.Add(forwardSquare);

                    // Two squares forward (only from starting row)
                    if (from.Row == startRow)
                    {
                        int doubleRow = from.Row + (2 * direction);
                        var doubleSquare = GetSquare(squares, doubleRow, from.Column);

                        if (doubleSquare != null && !doubleSquare.HasPiece)
                        {
                            moves.Add(doubleSquare);
                        }
                    }
                }
            }

            // Captures (diagonals)
            int[] captureCols = { from.Column - 1, from.Column + 1 };

            foreach (var col in captureCols)
            {
                if (col < 0 || col > 7)
                    continue;

                int row = from.Row + direction;

                if (row < 0 || row > 7)
                    continue;

                var target = GetSquare(squares, row, col);

                // Normal capture
                if (target != null &&
                    target.HasPiece &&
                    target.Piece.Colour != from.Piece.Colour)
                {
                    moves.Add(target);
                }

                // En passant capture
                if (enPassantTarget != null &&
                    enPassantTarget.Row == row &&
                    enPassantTarget.Column == col)
                {
                    moves.Add(enPassantTarget);
                }
            }

            return moves;
        }


        // Bishop Moves
        private List<SquareViewModel> GetBishopMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            // 4 diagonal directions
            int[] rowDirections = { 1, 1, -1, -1 };
            int[] colDirections = { 1, -1, 1, -1 };

            for (int d = 0; d < 4; d++)
            {
                int row = from.Row + rowDirections[d];
                int col = from.Column + colDirections[d];

                while (row >= 0 && row <= 7 && col >= 0 && col <= 7)
                {
                    var target = GetSquare(squares, row, col);

                    if (target == null)
                        break;

                    // If square has a piece
                    if (target.HasPiece)
                    {
                        // Opponent's piece -> can capture
                        if (target.Piece.Colour != from.Piece.Colour)
                        {
                            moves.Add(target);
                        }

                        // Always stop after hitting a piece (blocked)
                        break;
                    }

                    // Empty square -> valid move
                    moves.Add(target);

                    row += rowDirections[d];
                    col += colDirections[d];
                }
            }

            return moves;
        }


        // Rook Moves
        private List<SquareViewModel> GetRookMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            // 4 straight directions: up, down, left, right
            int[] rowDirections = { 1, -1, 0, 0 };
            int[] colDirections = { 0, 0, 1, -1 };

            for (int d = 0; d < 4; d++)
            {
                int row = from.Row + rowDirections[d];
                int col = from.Column + colDirections[d];

                while (row >= 0 && row <= 7 && col >= 0 && col <= 7)
                {
                    var target = GetSquare(squares, row, col);

                    if (target == null)
                        break;

                    // If square has a piece
                    if (target.HasPiece)
                    {
                        // Opponent's piece -> can capture
                        if (target.Piece.Colour != from.Piece.Colour)
                        {
                            moves.Add(target);
                        }

                        // Always stop after hitting a piece (blocked)
                        break;
                    }

                    // Empty square -> valid move
                    moves.Add(target);

                    row += rowDirections[d];
                    col += colDirections[d];
                }
            }

            return moves;
        }


        // Queen Moves
        private List<SquareViewModel> GetQueenMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            // Combine bishop + rook moves
            moves.AddRange(GetBishopMoves(from, squares));
            moves.AddRange(GetRookMoves(from, squares));

            return moves;
        }


        // King Moves
        private List<SquareViewModel> GetKingMoves(
            SquareViewModel from,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    // Skip current square
                    if (rowOffset == 0 && colOffset == 0)
                        continue;

                    int newRow = from.Row + rowOffset;
                    int newCol = from.Column + colOffset;

                    if (newRow < 0 || newRow > 7 || newCol < 0 || newCol > 7)
                        continue;

                    var target = GetSquare(squares, newRow, newCol);

                    if (target == null)
                        continue;

                    // Cannot move onto own piece
                    if (target.HasPiece &&
                        target.Piece.Colour == from.Piece.Colour)
                        continue;

                    moves.Add(target);
                }
            }

            // Castling
            moves.AddRange(GetCastlingMoves(from, squares));

            return moves;
        }

        // Castling Moves
        private List<SquareViewModel> GetCastlingMoves(
            SquareViewModel kingSquare,
            IEnumerable<SquareViewModel> squares)
        {
            var moves = new List<SquareViewModel>();

            var colour = kingSquare.Piece.Colour;
            int row = colour == PieceColour.White ? 7 : 0;

            // King must be on starting square
            if (kingSquare.Column != 4)
                return moves;

            // Kingside (short castle)
            var fSquare = GetSquare(squares, row, 5);
            var gSquare = GetSquare(squares, row, 6);
            var rookSquare = GetSquare(squares, row, 7);

            if (fSquare != null && gSquare != null && rookSquare != null &&
                !fSquare.HasPiece &&
                !gSquare.HasPiece &&
                rookSquare.HasPiece &&
                rookSquare.Piece.Type == PieceType.Rook)
            {
                moves.Add(gSquare);
            }

            // Queenside (long castle)
            var bSquare = GetSquare(squares, row, 1);
            var cSquare = GetSquare(squares, row, 2);
            var dSquare = GetSquare(squares, row, 3);
            var rookQ = GetSquare(squares, row, 0);

            if (bSquare != null && cSquare != null && dSquare != null &&
                !bSquare.HasPiece &&
                !cSquare.HasPiece &&
                !dSquare.HasPiece &&
                rookQ.HasPiece &&
                rookQ.Piece.Type == PieceType.Rook)
            {
                moves.Add(cSquare);
            }

            return moves;
        }

        #endregion

        #region Check Detection Methods
        // Helper to find the king's square for a given colour
        public SquareViewModel FindKing(IEnumerable<SquareViewModel> squares, PieceColour colour)
        {
            return squares.FirstOrDefault(s =>
                s.HasPiece &&
                s.Piece.Type == PieceType.King &&
                s.Piece.Colour == colour);
        }

        // Check if a square is under attack by any piece of the specified colour
        public bool IsSquareUnderAttack(
            IEnumerable<SquareViewModel> squares,
            int targetRow,
            int targetCol,
            PieceColour attackingColour)
        {
            foreach (var square in squares)
            {
                if (!square.HasPiece)
                    continue;

                if (square.Piece.Colour != attackingColour)
                    continue;

                var moves = GetMoves(square, squares);

                if (moves.Any(m => m.Row == targetRow && m.Column == targetCol))
                    return true;
            }

            return false;
        }

        // Check if the king of the specified colour is in check
        public bool IsKingInCheck(IEnumerable<SquareViewModel> squares, PieceColour colour)
        {
            var king = FindKing(squares, colour);

            if (king == null)
                return false;

            PieceColour enemyColour = colour == PieceColour.White
                ? PieceColour.Black
                : PieceColour.White;

            return IsSquareUnderAttack(squares, king.Row, king.Column, enemyColour);
        }
        #endregion

        // Helper to make a temporary move and return captured piece (if any) for undoing
        private (ChessPiece capturedPiece, SquareViewModel from, SquareViewModel to)
        MakeTemporaryMove(SquareViewModel from, SquareViewModel to)
        {
            var captured = to.Piece;

            to.SetPiece(from.Piece);
            from.ClearPiece();

            return (captured, from, to);
        }

        // Helper to undo a temporary move using the captured piece info
        private void UndoTemporaryMove(
            SquareViewModel from,
            SquareViewModel to,
            ChessPiece capturedPiece)
        {
            from.SetPiece(to.Piece);
            to.SetPiece(capturedPiece);
        }

    }
}
