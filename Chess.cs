using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DotStd
{
    public enum ChessColor
    {
        White = 0,
        Black = 1,
    }

    public enum ChessPieceType
    {
        // How might the piece move?
        King,       // K, 
        Queen,      // Q, 
        Bishop,     // B, 
        Knight,     // N, 
        Rook,       // R, 
        Pawn,       // c (or blank/space/nothing)
    }

    public enum ChessPieceId
    {
        // Board has Max 32 pieces.
        // ChessPieceType is constant to start but pawns may be upgraded. ChessPieceType

        WQR,    // a1 // Rook
        WQN,    // b1 // Knight
        WQB,    // c1 // Bishop
        WQ,     // Queen
        WK,     // King
        WKB,
        WKN,
        WKR,

        WPa,    // a2
        WPb,    // b2
        WPc,
        WPd,
        WPe,
        WPf,
        WPg,
        WPh,

        BQR,    // a8
        BQN,    // b8
        BQB,    // c8
        BQ,
        BK,
        BKB,
        BKN,
        BKR,

        BPa,    // a7
        BPb,    // b7
        BPc,
        BPd,
        BPe,
        BPf,
        BPg,
        BPh,
    }

    public struct ChessPosition
    {
        // A position on the board.
        // a1 = lower left of screen board.
        // queen on left side.
        // white = bottom row (1) (if you are starting)

        public byte X;    // 0-7 -> a-h
        public byte Y;    // 0-7 -> 1-8

        bool IsCaptured     // notation for a piece not currently on the board.
        {
            get
            {
                return X >= Chess.kD || Y >= Chess.kD;
            }
        }

        public string Notation
        {
            get
            {
                if (IsCaptured)
                {
                    return "x";
                }
                return string.Concat(X + 'a', Y + '1');
            }
        }

        public bool IsInArray(ChessPosition[] a)
        {
            foreach (var pos in a)
            {
                if (pos.X == this.X && pos.Y == this.Y)
                    return true;
            }
            return false;
        }

        public void SetCaptured()
        {
            // a piece is no longer on the board.
            X = Chess.kD;
            Y = Chess.kD;
        }

        public ChessPosition(byte x) // Chess.kD
        {
            // AKA SetCaptured.
            Debug.Assert(x == Chess.kD);
            X = x;
            Y = x;
        }
        public ChessPosition(byte x, byte y)
        {
            // create a Valid position on the board.
            X = x;
            Y = y;
            Debug.Assert(!IsCaptured);
        }
    }

    [Flags]
    public enum ChessFlag
    {
        // Result of a move or a test for move.

        OK = 0,     // Move is ok.
        CastleK,    // 0-0 = kings side castle. (short)
        CastleQ,    // 0-0-0 = queen side castle. (long)

        Upgrade,    // pawn upgrade.
        Capture,    // Captured another piece. 'x'

        Check,      // Move is blocked because it results in check. '+'
        Blocked,    // Cant move here because of block by own piece or out of bounds.
    }

    public class ChessPiece
    {
        public ChessPieceId Id;         // What piece am i?
        public ChessPieceType Type;     // What Type am i ? pawns may be upgraded.
        public ChessPosition Pos;       // My current position on board. test IsCaptured

        public ChessColor Color         // what side ? ChessColor
        {
            get
            {
                return Id > ChessPieceId.WPh ? ChessColor.Black : ChessColor.White;
            }
        }
    };

    public class ChessMove
    {
        // record history of moves.
        public ChessPieceId Id;
        public ChessPosition To;
        public ChessFlag Flag;  // Was this a Check, Castle, etc ?
    }

    public class Chess
    {
        // Store the current state of a chess game.
        // https://opensource.apple.com/source/Chess/Chess-110.0.6/Documentation/PGN-Standard.txt
        // https://www.expert-chess-strategies.com/chess-notation.html
        // https://database.chessbase.com/

        public const int kD = 8;    // board is a matrix of 8x8 (a-h)x(1-8)

        private ChessPiece[] piece;      // status of all pieces. ChessPieceId enum [32]

        // alternate views to pieces. indexes.
        // private ChessPiece[kD,kD] board;    // 2d array. null = unoccupied space. ChessPieceId
        // private List<ChessPiece> captured;    // list of pieces off board. ChessPieceId

        public int MoveNumber;  // Whose turn is it to move now ? 1 based. completed moves. Odd = white. e.g. 2 = waiting for black to move.
        public string White;    // name
        public string Black;    // name
        public DateTime FirstMove;  // when?
        public DateTime LastMove;   // when?

        // bool DeclaredInvalid; // Next turn is refused for invalid last move ? game is draw?

        public ChessColor MoveTurn
        {
            // Whose turn is it to move next?
            get
            {
                return ((MoveNumber % 1) == 1) ? ChessColor.White : ChessColor.Black;
            }
        }

        public void ResetBoard()
        {
            // Put all pieces back to start.
            piece = new ChessPiece[kD * 4];     // All pieces on both sides.
            // board = new ChessPiece[kD, kD]();

        }

        public ChessPiece GetPiece(ChessPieceId id)
        {
            return piece[(int)id];
        }
        public ChessPosition GetPosition(ChessPieceId id)
        {
            // Where is this ChessPiece on the board ?
            // return: null = captured.

            return new ChessPosition(0, 0);
        }

        public bool IsChecked(ChessColor color)
        {
            // Is ChessColor side in check?

            return false;
        }

        public ChessPosition[] GetValidMovesFor(ChessPieceId id)
        {
            // All Legal moves for a piece?
            var piece = GetPiece(id);

            // Remove moves that would put me in check?

            return null;
        }

        public bool IsValidMove(ChessPieceId id, ChessPosition pos)
        {
            var validMoves = GetValidMovesFor(id);
            if (validMoves == null)
                return false;
            return pos.IsInArray(validMoves);
        }

        private void MoveX(ChessPieceId id, ChessPosition pos)
        {
            // Move the piece with no tests for valid. Assume restore.

        }

        public bool Move(ChessPieceId id, ChessPosition posNew)
        {
            // ChessFlag ?

            var piece = GetPiece(id);
            if (piece.Color != MoveTurn)    // not my turn.
                return false;

            if (!IsValidMove(id, posNew))
                return false;

            var posOld = GetPosition(id);  // Previous pos.
            MoveX(id, posNew);

            MoveNumber++;
            return true;    // good.
        }
    }
}
