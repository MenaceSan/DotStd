using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;

// TODO : ChessRecommend.

namespace DotStd
{
    public enum ChessColorId
    {
        // Two sides/team/color to the chess game board.
        White = 0,
        Black = 1,
        Observer,   // I'm just an observer.
    }

    public enum ChessTypeId
    {
        // How might the piece move?
        Rook = 0,       // R, 
        Knight,     // N, 
        Bishop,     // B, 
        Queen,      // Q, 
        King,       // K, 
        Pawn,       // blank/space/nothing. may use the row (a-h)
    }

    public enum ChessPieceId
    {
        // Board has Max 32 pieces.
        // ChessPieceType is constant to start but pawns may be Promoted. ChessPieceType

        // White pieces. Move First.

        WQR = 0,    // a1 // Rook
        WQN,    // b1 // Knight
        WQB,    // c1 // Bishop
        WQ,     // Queen
        WK,     // King (+)
        WKB,
        WKN,
        WKR = 7,

        WPa,    // a2
        WPb,    // b2
        WPc,
        WPd,
        WPe,
        WPf,
        WPg,
        WPh = 15,

        // Black pieces.

        BQR,    // a8
        BQN,    // b8
        BQB,    // c8
        BQ,
        BK,
        BKB,
        BKN,
        BKR = 23,

        BPa,    // a7
        BPb,    // b7
        BPc,
        BPd,
        BPe,
        BPf,
        BPg,
        BPh,

        QTY = 32,
    }

    public struct ChessOffset
    {
        // A delta position move for a type of piece.
        public sbyte dx;
        public sbyte dy;

        public const sbyte kCastle = 2;     // Castle will offset the king this many spaces.

        public ChessOffset(sbyte _dx, sbyte _dy) { dx = _dx; dy = _dy; }
    }

    public struct ChessPosition : IEquatable<ChessPosition>
    {
        // A position on the board.
        // a1 = lower left of screen board.
        // queen on left side.
        // white = bottom row (1) (if you are starting)

        public byte X;    // 0-7 -> a-h = file.
        public byte Y;    // 0-7 -> 1-8 = row/rank

        public const byte kDim = 8;    // board is a matrix of 8x8 (a-h)x(1-8)
        public const byte kDim1 = kDim - 1;
        public const byte kDim2 = kDim - 2;

        public const char kX0 = 'a';    // file.
        public const char kY0 = '1';

        public const byte kXK = 4;      // King position.
        public const byte kXCQR = kXK - 1;    // Castle queen side rook to here.
        public const byte kXCKR = kXK + 1;    // Castle king side rook to here.

        public bool IsOnBoard => X < kDim && Y < kDim;    // notation for a piece not currently on the board. Invalid position. IsCaptured
        public bool IsCaptured => !IsOnBoard;    // notation for a piece not currently on the board. Invalid position.

        public string Notation  // Where is this piece now.
        {
            get
            {
                if (IsCaptured)
                {
                    return "x";
                }
                return string.Concat((char)(kX0 + X), (char)(kY0 + Y));
            }
        }

        public static bool IsXFile(char ch)
        {
            return ch >= kX0 && ch <= 'h';
        }
        public static bool IsYRank(char ch)
        {
            return ch >= '1' && ch <= '8';
        }

        public void SetCaptured(int capCount)
        {
            // a piece is no longer on the board. IsCaptured
            X = kDim;
            Y = (byte)capCount;     // order of capture.
        }

        public static byte Offset1(byte v, sbyte o)
        {
            // Get position + offset.
            return (byte)(v + o);
        }
        public ChessPosition Offset(sbyte dx, sbyte dy)
        {
            // Get position + offset.
            return new ChessPosition(Offset1(X, dx), Offset1(Y, dy));
        }
        public ChessPosition Offset(ChessOffset d)
        {
            // Get position + offset.
            return Offset(d.dx, d.dy);
        }

        public bool Equals(ChessPosition other)
        {
            // IEquatable
            return X == other.X && Y == other.Y;
        }

        public static char GetCharX(byte x)
        {
            // Letter
            return (char)(x < 26 ? (x + 'a') : ((x - 26) + '1'));
        }
        public static char GetCharY(byte y)
        {
            // Number
            return (char)(y < 10 ? (y + '1') : ((y - 10) + 'a'));
        }

        public ChessPosition(byte x, byte y)
        {
            // create a Valid position on the board.
            X = x;
            Y = y;
        }
        public ChessPosition(char cxFile, char cyRank)
        {
            // create a Valid position on the board.
            X = (byte)(cxFile - kX0);
            Y = (byte)(cyRank - kY0);
        }
    }

    public class ChessColor
    {
        // Define features that are color specific.
        // team/color/side

        public readonly ChessColorId Id;
        private readonly bool IsWhite;

        public static readonly ChessColor kWhite = new ChessColor(ChessColorId.White);
        public static readonly ChessColor kBlack = new ChessColor(ChessColorId.Black);

        public sbyte Dir => (sbyte)(IsWhite ? 1 : -1);  // What direction do pawns move?

        public ChessPieceId KingId => IsWhite ? ChessPieceId.WK : ChessPieceId.BK;

        public byte RowKing => (byte)(IsWhite ? 0 : ChessPosition.kDim1); // Get the kings rank/row. 0

        public byte RowPawn => (byte)(IsWhite ? 1 : ChessPosition.kDim2); // Get the pawn rank/row. 1

        public byte RowPromote => (byte)(IsWhite ? ChessPosition.kDim1 : 0);   // Promote the pawn rank/row. ChessFlags.Promote

        public ChessCastleFlags CastleFlags => IsWhite ? (ChessCastleFlags.WQ | ChessCastleFlags.WK) : (ChessCastleFlags.BQ | ChessCastleFlags.BK);

        public ChessColor Opposite => IsWhite ? kBlack : kWhite;

        public ChessCastleFlags GetCastleFlags(bool isQueenSide)
        {
            if (IsWhite)
            {
                return isQueenSide ? ChessCastleFlags.WQ : ChessCastleFlags.WK;
            }
            else
            {
                return isQueenSide ? ChessCastleFlags.BQ : ChessCastleFlags.BK;
            }
        }

        public ChessPieceId GetRookId(bool isQueenSide)
        {
            // Get Queen/King side rook.
            if (IsWhite)
            {
                return isQueenSide ? ChessPieceId.WQR : ChessPieceId.WKR;
            }
            else
            {
                return isQueenSide ? ChessPieceId.BQR : ChessPieceId.BKR;
            }
        }

        static readonly ChessOffset[] _MovesPawnW = new ChessOffset[]
        {
            new ChessOffset(-1,1),
            new ChessOffset(0,1),
            new ChessOffset(1,1),
        };
        static readonly ChessOffset[] _MovesPawnB = new ChessOffset[]
        {
            new ChessOffset(-1,-1),
            new ChessOffset(0,-1),
            new ChessOffset(1,-1),
        };
        public ChessOffset[] PawnMoves => IsWhite ? _MovesPawnW : _MovesPawnB;

        public ChessColor(ChessColorId colorId)
        {
            Id = colorId;
            IsWhite = colorId == ChessColorId.White;
        }
    }

    [Flags]
    public enum ChessFlags
    {
        // Result of a move or a test for move.

        OK = 0,     // Move is OK.
        CastleK = 0x0001,       // 0-0 = kings side castle. (short)
        CastleQ = 0x0002,       // 0-0-0 = queen side castle. (long)

        Capture = 0x0010,       // Move will captured another piece. 'x'
        Check = 0x0020,         // Results in check on opposite colors king. '+'
        Promote = 0x0040,       // Move will Promote pawn. (to queen i assume?) upgrade.
        Checkmate = 0x0080,     // Game is over.

        Invalid = 0x01000,      // Invalid move. Not a valid move for this type of piece.
        Blocked = 0x0200,       // Cant move here because of block by own piece or out of bounds.
        CheckBlock = 0x0400,    // Move is blocked because it results in check on me.

        Captured = 0x1000,      // This piece is captured and off the board. Can't move. Why are you trying this ?
        Resigned = 0x2000,      // Last player resigned. Game is over. Was probably check mated.
        // Contested = 0x4000,   // Next turn is refused for invalid last move ? game is draw?
    }

    public enum ChessCastleFlags
    {
        // Record that castle has been used. And cant be used again.
        WK = 0x0001,    // 0-0 = kings side castle. (short)
        WQ = 0x0002,    // 0-0-0 = queen side castle. (long)
        BK = 0x0004,    // 0-0 = kings side castle. (short)
        BQ = 0x0008,    // 0-0-0 = queen side castle. (long)
    }

    public class ChessType
    {
        // What types of moves can this piece make?

        static readonly ChessOffset[] _MovesKing = new ChessOffset[]
        {
            new ChessOffset(1,1),
            new ChessOffset(1,0),
            new ChessOffset(1,-1),
            new ChessOffset(0,-1),
            new ChessOffset(-1,-1),
            new ChessOffset(-1,0),
            new ChessOffset(-1,1),
            new ChessOffset(0,1),
        };
        static readonly ChessOffset[] _MovesRook = new ChessOffset[]
        {
            new ChessOffset(1,0),
            new ChessOffset(0,-1),
            new ChessOffset(-1,0),
            new ChessOffset(0,1),
        };
        static readonly ChessOffset[] _MovesBishop = new ChessOffset[]
        {
            new ChessOffset(1,1),
            new ChessOffset(1,-1),
            new ChessOffset(-1,-1),
            new ChessOffset(-1,1),
        };
        static readonly ChessOffset[] _MovesKnight = new ChessOffset[]
        {
            new ChessOffset(-1,2),
            new ChessOffset(1,2),
            new ChessOffset(2,1),
            new ChessOffset(2,-1),
            new ChessOffset(1,-2),
            new ChessOffset(-1,-2),
            new ChessOffset(-2,1),
            new ChessOffset(-2,-1),
        };

        public ChessOffset[] Offsets;
        public int Spaces;

        public const string kTypeChars = "RNBQK";
        internal static char GetTypeLetter(ChessTypeId type)
        {
            // avoid use of Pawn here.
            return kTypeChars[(int)type];
        }
        internal static ChessTypeId GetTypeFrom(char ch)
        {
            // -1 = not here.
            if (ChessPosition.IsXFile(ch))
                return ChessTypeId.Pawn;
            int id = kTypeChars.IndexOf(ch);
            return (ChessTypeId)id;      // may return -1;
        }

        public static ChessType GetType(ChessTypeId type, ChessColor color)
        {
            // How might each type move?
            switch (type)
            {
                case ChessTypeId.Pawn:   // Weird moves.
                    return new ChessType { Offsets = color.PawnMoves, Spaces = 1 };

                case ChessTypeId.King:
                    return new ChessType { Offsets = _MovesKing, Spaces = 1 };

                case ChessTypeId.Rook:
                    return new ChessType { Offsets = _MovesRook, Spaces = ChessPosition.kDim };

                case ChessTypeId.Knight: // Knight has 8 possible places to go.
                    return new ChessType { Offsets = _MovesKnight, Spaces = 1 };

                case ChessTypeId.Bishop:
                    return new ChessType { Offsets = _MovesBishop, Spaces = ChessPosition.kDim };

                case ChessTypeId.Queen:
                    return new ChessType { Offsets = _MovesKing, Spaces = ChessPosition.kDim };

                default:
                    return null;
            }
        }

        internal static int GetTypeValue(ChessTypeId type)
        {
            // What is this piece type worth ? 30 + king
            // https://www.chess.com/article/view/chess-piece-value

            switch (type)
            {
                case ChessTypeId.Pawn:   // Weird moves.
                    return 1; // *8 = 8
                case ChessTypeId.King:
                    return 31;  // shouldn't really matter.
                case ChessTypeId.Rook:
                    return 5; // *2 = 10
                case ChessTypeId.Knight:
                case ChessTypeId.Bishop:
                    return 3; // *2 = 12
                case ChessTypeId.Queen:
                    return 9;
                default:
                    return 0;
            }
        }
    }

    public class ChessPiece
    {
        // a piece on the board.

        public readonly ChessPieceId Id;         // What piece am i?
        public ChessTypeId Type;     // What Type am i ? pawns may be Promoted.
        public ChessPosition Pos;       // My current position on board. test IsCaptured

        private bool IsWhite => Id <= ChessPieceId.WPh;
        public ChessColor Color => IsWhite ? ChessColor.kWhite : ChessColor.kBlack;        // what side ?  

        public bool IsCaptured => Pos.IsCaptured;
        public bool IsKing => Type == ChessTypeId.King;

        public int Value => ChessType.GetTypeValue(Type);
        public int ValueChange => IsWhite ? Value : -Value;     // ASSUME IsCaptured   

        public bool IsOpeningPawn => Type == ChessTypeId.Pawn && Pos.Y == Color.RowPawn;  // is this a pawn in its starting position?

        public static bool HasAnyFlag(ChessFlags flags, ChessFlags flagsTest)
        {
            return (flags & flagsTest) != 0;
        }

        public static bool IsAllowedMove(ChessFlags flags)
        {
            return !HasAnyFlag(flags, ChessFlags.Invalid | ChessFlags.Blocked | ChessFlags.CheckBlock);
        }

        public static bool IsQueenSideRook(ChessPieceId id)
        {
            return id == ChessPieceId.WQR || id == ChessPieceId.BQR;
        }

        public ChessPiece(ChessPiece clone)
        {
            Id = clone.Id;
            Type = clone.Type;
            Pos = clone.Pos;
        }
        public ChessPiece(ChessPieceId id, ChessTypeId type, ChessPosition pos)
        {
            Id = id;
            Type = type;
            Pos = pos;
        }
    };

    public class ChessBoard
    {
        // The status of the board.
        // Validate moves that might be made.

        public ChessPiece[] Pieces;      // status of all 32 pieces by Id. ChessPieceId enum [32 = ChessPieceId.QTY = kDim * 4]. captured or in play on board.
        private byte[,] Squares;    // 8x8 2d array. ChessPieceId.QTY = unoccupied space. ChessPieceId [kDim][kDim]

        public int CaptureCount;    // Quantity of pieces off board. 
        public int Score;           // 0 = balanced game, + = white advantage.
        public ChessCastleFlags CastleFlags;     // Have we moved a rook or king to prevent future castle.

        public ChessPiece GetPiece(ChessPieceId id)
        {
            return Pieces[(int)id];
        }

        private ChessPiece GetAt(byte x, byte y)
        {
            byte idb = Squares[x, y];
            if (idb == 0)
                return null;
            return GetPiece((ChessPieceId)(idb - 1));
        }
        public ChessPiece GetAt(ChessPosition pos)
        {
            if (!pos.IsOnBoard)
                return null;
            return GetAt(pos.X, pos.Y);
        }
        private void SetAt(ChessPosition pos, byte idb)
        {
            Squares[pos.X, pos.Y] = idb;
        }
        private void SetAt(ChessPosition pos, ChessPiece piece)
        {
            SetAt(pos, (byte)((int)piece.Id + 1));
            piece.Pos = pos;
        }

        public static bool IsOffsetSign(sbyte offset1, int dv)
        {
            // does the single dimension offset match direction?
            switch (offset1)
            {
                case 0: return dv == 0;
                case 1: return dv > 0;
                case -1: return dv < 0;
            }
            Debug.Assert(false);
            return false;
        }
        public static bool IsOffsetDir(ChessOffset offset, int dx, int dy)
        {
            // does the offset match direction? vector 2.
            return IsOffsetSign(offset.dx, dx) && IsOffsetSign(offset.dy, dy);
        }

        public bool IsCastleable(ChessColor color, bool isQueenSide)
        {
            // Can i do a castle?
            // MUST not be in check now! ASSUME already checked that.
            // https://en.wikipedia.org/wiki/Castling

            ChessCastleFlags flags = color.GetCastleFlags(isQueenSide);
            if ((CastleFlags & flags) != 0)     // already moved 
                return false;

            byte y0 = color.RowKing;

            ChessPiece king = GetPiece(color.KingId);
            if (king.Pos.Y != y0 || king.Pos.X != ChessPosition.kXK)
            {
                // CastleFlags FAILED ME!
                CastleFlags |= color.CastleFlags;
                return false;
            }

            byte xR = (byte)(isQueenSide ? 0 : (ChessPosition.kDim1)); // in rook starting pos.
            ChessPieceId idR = color.GetRookId(isQueenSide);

            ChessPiece rook = GetPiece(idR);
            if (rook.Pos.Y != y0 && rook.Pos.X != xR)
            {
                // CastleFlags FAILED ME!
                CastleFlags |= flags;
                return false;
            }

            // Must have empty spaces ?
            byte x1 = ChessPosition.kXK + 1;
            byte x2 = xR;
            if (isQueenSide)
            {
                x1 = 1; x2 = ChessPosition.kXK;
            }
            for (byte x = x1; x < x2; x++)
            {
                if (GetAt(x, y0) != null)
                    return false;
            }

            return true;    // Assume move will revert if this puts me in check.
        }

        public bool IsValidBoard()
        {
            // Is the game state valid ?

            int score = 0;
            int capCount = 0;
            int capSum = 0;
            int id = (int)ChessPieceId.WQR;
            foreach (var piece in Pieces)
            {
                if (piece.Id != (ChessPieceId)id)
                    return false;
                if (piece.IsCaptured)
                {
                    capSum += piece.Pos.Y;  // stored CaptureCount
                    capCount++;
                    score += piece.ValueChange;
                }
                else
                {
                    if (GetAt(piece.Pos) != piece)
                        return false;
                }

                id++;
            }

            List<ChessPiece> pieces = Pieces.ToList();
            for (byte y = 0; y < ChessPosition.kDim; y++)
            {
                for (byte x = 0; x < ChessPosition.kDim; x++)
                {
                    ChessPiece piece = GetAt(x, y);
                    if (piece == null)
                        continue;
                    if (piece.Pos.X != x || piece.Pos.Y != y || piece.IsCaptured)
                        return false;
                    pieces.Remove(piece);   // accounted for.
                }
            }

            // All pieces NOT on the board are considered captured.
            foreach (var piece in pieces)
            {
                if (!piece.IsCaptured)
                    return false;
            }

            int capSum2 = (capCount * (capCount + 1)) / 2;
            if (CaptureCount != capCount || capSum != capSum2)
                return false;
            if (score != Score)
                return false;

            return true;
        }

        public ChessFlags TestMove1(ChessPiece piece, ChessPosition posNew)
        {
            // No check of distance moved. Can i go on this space?

            if (!posNew.IsOnBoard)  // can't move off the board.
                return ChessFlags.Invalid;

            ChessPiece pieceCapture = GetAt(posNew);
            Debug.Assert(piece != pieceCapture);

            ChessFlags flags;
            if (pieceCapture == null)
            {
                if (piece.Type == ChessTypeId.Pawn && piece.Pos.X != posNew.X)  // pawn is blocked forward.
                    return ChessFlags.Invalid;
                flags = ChessFlags.OK;
            }
            else
            {
                if (pieceCapture.Color == piece.Color)  // cant capture my own side
                    return ChessFlags.Blocked;
                if (piece.Type == ChessTypeId.Pawn && piece.Pos.X == posNew.X)  // must capture to move diagonal.
                    return ChessFlags.Blocked;
                flags = ChessFlags.Capture;
                if (pieceCapture.IsKing)
                    flags |= ChessFlags.Check;  // assume this is just a test. 
            }

            if (piece.Type == ChessTypeId.Pawn && piece.Color.RowPromote == piece.Pos.Y)    // Is this a pawn Promote event?
            {
                flags |= ChessFlags.Promote;
            }

            return flags;
        }

        public ChessFlags TestMove(ChessPiece piece, ChessPosition posNew, bool isInCheck)
        {
            // Is this move OK? and return what will happen as ChessFlags.
            // NOTE: Does not check moves that would put me in check.

            if (piece.IsCaptured)
                return ChessFlags.Captured;  // not allowed to move.
            if (!posNew.IsOnBoard)  // can't move off the board.
                return ChessFlags.Blocked;

            ChessType type = ChessType.GetType(piece.Type, piece.Color);
            if (type == null)
                return ChessFlags.Invalid;

            ChessPosition posTest = piece.Pos;
            int dx = posNew.X - posTest.X;
            int dy = posNew.Y - posTest.Y;
            ChessFlags flags = ChessFlags.Invalid;

            if (piece.IsKing && !isInCheck)
            {
                if (dx == -ChessOffset.kCastle && IsCastleable(piece.Color, true)) // Queenside
                {
                    return ChessFlags.CastleQ;
                }
                if (dx == ChessOffset.kCastle && IsCastleable(piece.Color, false)) // Kingside
                {
                    return ChessFlags.CastleK;
                }
            }

            foreach (var offset in type.Offsets)
            {
                if (offset.dx == dx && offset.dy == dy)   // exact match.
                {
                    flags = TestMove1(piece, posNew);
                    break;
                }
                int spaces = type.Spaces;
                if (dx == 0 && offset.dx == 0 && dy == offset.dy * 2 && piece.IsOpeningPawn)    // Special pawn opening move.
                {
                    spaces = 2; // can move 2 spaces.
                }

                if (spaces > 1 && IsOffsetDir(offset, dx, dy))
                {
                    // Move toward target.
                    posTest = piece.Pos;
                    for (int i = 0; i < spaces; i++)
                    {
                        posTest = posTest.Offset(offset);
                        flags = TestMove1(piece, posTest);
                        if (!ChessPiece.IsAllowedMove(flags))
                            return flags;
                        if (posTest.Equals(posNew))
                            break;
                    }

                    break;  // can be only 1. so stop looking.
                }
            }

            return flags;
        }

        public bool IsInDanger(ChessPiece king)
        {
            // Given the current board state. Is the King in danger ?

            foreach (var piece in Pieces)
            {
                if (piece.Color == king.Color)  // skip kings own pieces.
                    continue;
                ChessFlags flags = TestMove(piece, king.Pos, false);
                if (ChessPiece.HasAnyFlag(flags, ChessFlags.Capture))  // someone could capture the king !
                    return true;
            }

            return false;
        }

        bool TestCheckmate(ChessColor color)
        {
            // test for ChessFlags.Checkmate
            // Is there any move they could make that would get out of check?

            foreach (var piece2 in Pieces)
            {
                if (piece2.IsCaptured || piece2.Color == color)
                    continue;
                var moves = GetValidMovesFor(piece2.Id, true);
                if (moves.Count > 0)
                {
                    return false; // some move will get them out of check!
                }
            }
            return true;    // checkmate. // Has no possible moves
        }

        public ChessFlags MoveX(ChessPiece piece, ChessPosition posNew, bool testMove, bool isInCheck)
        {
            // Make a move. revert it in case of test.
            // ChessFlags = result of move. (or failure)
            // testMove = i already know this is basically valid but test it for check

            ChessFlags newFlags = testMove ? ChessFlags.OK : TestMove(piece, posNew, isInCheck);
            if (!ChessPiece.IsAllowedMove(newFlags))
            {
                return newFlags;
            }

            ChessColor color = piece.Color;
            int scoreChange = 0;
            ChessPosition posOld = piece.Pos;

            ChessPiece pieceCapture = GetAt(posNew);
            if (pieceCapture != null)
            {
                // A capture!
                Debug.Assert(pieceCapture.Color != color);
                Debug.Assert(testMove || !pieceCapture.IsKing);   // NOT allowed!
                newFlags |= ChessFlags.Capture;
                pieceCapture.Pos.SetCaptured(++CaptureCount);
                scoreChange = pieceCapture.ValueChange;
            }

            SetAt(posOld, 0);   // clear previous spot.
            SetAt(posNew, piece);

            ChessPieceId kingIdOp = color.Opposite.KingId; // other sides king.

            if (IsInDanger(GetPiece(color.KingId)))
            {
                // I can't move to some place that puts me in check!               
                newFlags |= ChessFlags.CheckBlock;
                testMove = true;  // revert this move.
            }
            else if (IsInDanger(GetPiece(kingIdOp)))
            {
                newFlags |= ChessFlags.Check;    // I put other King in check! good.
            }

            if (testMove)
            {
                // revert my test move.

                if (ChessPiece.HasAnyFlag(newFlags, ChessFlags.Check) && TestCheckmate(color))    // I put other King in check! good. is it checkmate?
                {
                    // NOTE: This does not accurately test castle move
                    newFlags |= ChessFlags.Checkmate;   // Has no possible moves
                }

                SetAt(posOld, piece);
                if (pieceCapture != null)
                {
                    CaptureCount--;
                    SetAt(posNew, pieceCapture);
                    scoreChange = 0;
                }
                else
                {
                    SetAt(posNew, 0);
                }
            }
            else
            {
                if (piece.IsKing)
                {
                    if (ChessPiece.HasAnyFlag(newFlags, ChessFlags.CastleK | ChessFlags.CastleQ))
                    {
                        // Castle. Move the rook as well!
                        bool isQueenSide = ChessPiece.HasAnyFlag(newFlags, ChessFlags.CastleQ);
                        ChessPieceId idR = color.GetRookId(isQueenSide);
                        ChessPiece pieceR = GetPiece(idR);

                        SetAt(pieceR.Pos, 0);   // clear previous spot.
                        SetAt(new ChessPosition(isQueenSide ? ChessPosition.kXCQR : ChessPosition.kXCKR, pieceR.Pos.Y), pieceR);

                        // Does this move put the other side in check?
                        ChessFlags flagsR = TestMove(pieceR, GetPiece(kingIdOp).Pos, false);
                        if (ChessPiece.HasAnyFlag(flagsR, ChessFlags.Capture))  // i could capture king.
                        {
                            newFlags |= ChessFlags.Check;
                        }
                    }
                    CastleFlags |= color.CastleFlags; // move king = can no longer castle.
                }

                if (piece.Type == ChessTypeId.Rook)
                {
                    CastleFlags |= color.GetCastleFlags(ChessPiece.IsQueenSideRook(piece.Id));
                }

                if (ChessPiece.HasAnyFlag(newFlags, ChessFlags.Check) && TestCheckmate(color))    // I put other King in check! good. is it checkmate?
                {
                    newFlags |= ChessFlags.Checkmate;   // Has no possible moves
                }
            }

            Score += scoreChange;

            return newFlags;
        }

        private List<ChessPosition> GetValidMovesFor(ChessPiece piece, bool isInCheck)
        {
            // what moves can i make.
            // NOTE: Does not check moves that would put me in check.

            if (piece.IsCaptured)
                return null;  // not allowed.

            ChessColor color = piece.Color;
            var possibles = new List<ChessPosition>();
            ChessPosition posOld = piece.Pos;
            ChessType type = ChessType.GetType(piece.Type, color);
            if (type == null)
                return null;

            if (piece.IsKing && !isInCheck)
            {
                if (IsCastleable(color, true)) // Queenside
                {
                    possibles.Add(posOld.Offset(-ChessOffset.kCastle, 0));
                }
                if (IsCastleable(color, false)) // Kingside
                {
                    possibles.Add(posOld.Offset(ChessOffset.kCastle, 0));
                }
            }

            foreach (var offset in type.Offsets)
            {
                ChessPosition posNew = posOld;
                int spaces = type.Spaces;

                if (offset.dx == 0 && piece.IsOpeningPawn)
                {
                    spaces = 2; // pawn opening move.
                }

                for (int i = 0; i < spaces; i++)
                {
                    posNew = posNew.Offset(offset);
                    ChessFlags flags = TestMove1(piece, posNew);
                    if (!ChessPiece.IsAllowedMove(flags))
                        break;
                    possibles.Add(posNew);
                    if (ChessPiece.HasAnyFlag(flags, ChessFlags.Capture))   // stop here.
                        break;
                }
            }

            return possibles;    // no moves.
        }

        public List<ChessPosition> GetValidMovesFor(ChessPieceId id, bool isInCheck)
        {
            // List all Legal moves for a piece?
            var piece = GetPiece(id);
            List<ChessPosition> ret = GetValidMovesFor(piece, isInCheck);   // Get all basically valid (non-check tested) moves

            // remove all positions that would put me in check.
            for (int i = 0; i < ret.Count; i++)
            {
                var posTest = ret[i];
                ChessFlags flags = MoveX(piece, posTest, true, false);      // only test for check and then revert.
                if (!ChessPiece.IsAllowedMove(flags))
                {
                    ret.RemoveAt(i);    // was not a valid move.
                    i--;
                }
            }

            return ret;
        }

        public new string ToString()
        {
            // Get the board state as a single string.
            var sb = new StringBuilder();
            foreach (var piece in Pieces)
            {
                sb.Append(ChessPosition.GetCharX(piece.Pos.X));
                sb.Append(ChessPosition.GetCharY(piece.Pos.Y));
            }
            return sb.ToString();
        }

        public static readonly ChessPiece[] _InitPieces = new ChessPiece[] // All pieces on both sides.
          {
            new ChessPiece(ChessPieceId.WQR, ChessTypeId.Rook,   new ChessPosition(0,0)),
            new ChessPiece(ChessPieceId.WQN, ChessTypeId.Knight, new ChessPosition(1,0)),
            new ChessPiece(ChessPieceId.WQB, ChessTypeId.Bishop, new ChessPosition(2,0)),
            new ChessPiece(ChessPieceId.WQ, ChessTypeId.Queen,   new ChessPosition(3,0)),
            new ChessPiece(ChessPieceId.WK, ChessTypeId.King,    new ChessPosition(4,0)),
            new ChessPiece(ChessPieceId.WKB, ChessTypeId.Bishop, new ChessPosition(5,0)),
            new ChessPiece(ChessPieceId.WKN, ChessTypeId.Knight, new ChessPosition(6,0)),
            new ChessPiece(ChessPieceId.WKR, ChessTypeId.Rook,   new ChessPosition(7,0)),

            new ChessPiece(ChessPieceId.WPa, ChessTypeId.Pawn,   new ChessPosition(0,1)),
            new ChessPiece(ChessPieceId.WPb, ChessTypeId.Pawn,   new ChessPosition(1,1)),
            new ChessPiece(ChessPieceId.WPc, ChessTypeId.Pawn,   new ChessPosition(2,1)),
            new ChessPiece(ChessPieceId.WPd, ChessTypeId.Pawn,   new ChessPosition(3,1)),
            new ChessPiece(ChessPieceId.WPe, ChessTypeId.Pawn,   new ChessPosition(4,1)),
            new ChessPiece(ChessPieceId.WPf, ChessTypeId.Pawn,   new ChessPosition(5,1)),
            new ChessPiece(ChessPieceId.WPg, ChessTypeId.Pawn,   new ChessPosition(6,1)),
            new ChessPiece(ChessPieceId.WPh, ChessTypeId.Pawn,   new ChessPosition(7,1)),

            new ChessPiece(ChessPieceId.BQR, ChessTypeId.Rook,   new ChessPosition(0,7)),
            new ChessPiece(ChessPieceId.BQN, ChessTypeId.Knight, new ChessPosition(1,7)),
            new ChessPiece(ChessPieceId.BQB, ChessTypeId.Bishop, new ChessPosition(2,7)),
            new ChessPiece(ChessPieceId.BQ, ChessTypeId.Queen,   new ChessPosition(3,7)),
            new ChessPiece(ChessPieceId.BK, ChessTypeId.King,    new ChessPosition(4,7)),
            new ChessPiece(ChessPieceId.BKB, ChessTypeId.Bishop, new ChessPosition(5,7)),
            new ChessPiece(ChessPieceId.BKN, ChessTypeId.Knight, new ChessPosition(6,7)),
            new ChessPiece(ChessPieceId.BKR, ChessTypeId.Rook,   new ChessPosition(7,7)),

            new ChessPiece(ChessPieceId.BPa, ChessTypeId.Pawn,   new ChessPosition(0,6)),
            new ChessPiece(ChessPieceId.BPb, ChessTypeId.Pawn,   new ChessPosition(1,6)),
            new ChessPiece(ChessPieceId.BPc, ChessTypeId.Pawn,   new ChessPosition(2,6)),
            new ChessPiece(ChessPieceId.BPd, ChessTypeId.Pawn,   new ChessPosition(3,6)),
            new ChessPiece(ChessPieceId.BPe, ChessTypeId.Pawn,   new ChessPosition(4,6)),
            new ChessPiece(ChessPieceId.BPf, ChessTypeId.Pawn,   new ChessPosition(5,6)),
            new ChessPiece(ChessPieceId.BPg, ChessTypeId.Pawn,   new ChessPosition(6,6)),
            new ChessPiece(ChessPieceId.BPh, ChessTypeId.Pawn,   new ChessPosition(7,6)),
          };

        private void InitBoard()
        {
            Squares = new byte[ChessPosition.kDim, ChessPosition.kDim];
            int id = (int)ChessPieceId.WQR;
            foreach (var piece in Pieces)
            {
                Debug.Assert(piece.Id == (ChessPieceId)id);
                if (!piece.IsCaptured)
                {
                    SetAt(piece.Pos, piece);
                }
                id++;
            }
            // ASSUME IsValidBoard();
        }

        private void InitPiecesAll(ChessPiece[] pieces)
        {
            Pieces = new ChessPiece[(int)ChessPieceId.QTY];
            for (int i = 0; i < (int)ChessPieceId.QTY; i++)
            {
                Pieces[i] = new ChessPiece(pieces[i]);    // clone ChessPiece
            }
            InitBoard();
        }

        public ChessBoard(string s)
        {
            // Set the board state as a single string.
            // decode string.

            // TODO
        }

        public ChessBoard(ChessPiece[] pieces)
        {
            // Take some pieces. NOT a clone.
            Pieces = new ChessPiece[(int)ChessPieceId.QTY];

            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                Pieces[(int)piece.Id] = piece;
                if (piece.IsCaptured)
                {
                    ++CaptureCount;
                    Score += piece.ValueChange;
                }
            }

            if (pieces.Length < (int)ChessPieceId.QTY)
            {
                // Assume any missing pieces are captured.
                for (int i = 0; i < (int)ChessPieceId.QTY; i++)
                {
                    var piece = Pieces[i];
                    if (piece == null)
                    {
                        Pieces[i] = piece = new ChessPiece(_InitPieces[i]);    // clone ChessPiece
                        piece.Pos.SetCaptured(++CaptureCount);
                        Score += piece.ValueChange;
                    }
                }
            }

            InitBoard();
        }

        public ChessBoard(ChessBoard clone)
        {
            // Make a clone copy of the state of this board so i can try other moves?
            // like ICloneable object
            InitPiecesAll(clone.Pieces);
            CaptureCount = clone.CaptureCount;
            Score = clone.Score;
            CastleFlags = clone.CastleFlags;
        }

        public ChessBoard()
        {
            // Create a new board.
            InitPiecesAll(_InitPieces);
        }
    }

    public class ChessRecommend
    {
        // recommended the next move for a color.
        // Predict, Suggest, Recommend.

        public ChessPieceId Id;     // move piece with the best score.
        public ChessPosition Pos;
        public ChessFlags Flags;    // What is the result of this move.
        // public int Score;  // What is the score at this level.
        // public float ScoreChildren;   // What may happen after this + n levels.

        public static ChessRecommend FindBest(ChessBoard board, ChessColor color, bool isInCheck, int depth = 5, int numThreads = 1)
        {
            // Find the best scoring move for the given board and color. 
            // This is CPU bound so async will not help us. only hard threads at the first level can.
            // depth = recurse this many times.
            // numThreads = break up the test into this many threads.

            // TODO

            if (depth <= 0)
                return null;

            var possibleMoves = new List<ChessRecommend>();   // All possible moves for this board and color at this board state.

            foreach (var piece in board.Pieces)
            {
                if (piece.IsCaptured || piece.Color != color)
                    continue;
                var moves = board.GetValidMovesFor(piece.Id, isInCheck);
                foreach (var move in moves)
                {
                    possibleMoves.Add(new ChessRecommend { Id = piece.Id, Pos = move });
                }
            }

            // Now run the test on as many threads as i like.
            // Which is the best?

            foreach (var move in possibleMoves)
            {

            }

            return null;
        }
    }

    [Serializable]
    public class ChessGameInfo
    {
        public int MoveNumber;  // Whose turn is it to move now ? 1 based. completed moves. Odd = white. e.g. 2 = waiting for black to move.
        public string White;    // color/side name
        public string Black;    // color/side name

        public DateTime LastMove;   // when?
        public DateTime FirstMove;  // when?
        public TimeSpan WhiteTime;  // How much time waiting for White?

        public ChessFlags GameState;     // Current state of the board. (Check, etc)

        public bool IsWhiteTurn => ((MoveNumber & 1) == 1);  // Whose turn is it to move next/now? White moves first. 1 based.
        public ChessColor TurnColor => IsWhiteTurn ? ChessColor.kWhite : ChessColor.kBlack;  // Whose turn is it to move next/now? White moves first. 1 based.

        public bool IsInCheck => ChessPiece.HasAnyFlag(GameState, ChessFlags.Check | ChessFlags.Checkmate);      // The current MoveTurn is in check and MUST get out or resign.

        public ChessGameInfo()
        {
            MoveNumber = 1;
            White = "White";    // name
            Black = "Black";    // name
            LastMove = DateTime.MinValue;  // when?
            FirstMove = DateTime.MinValue;  // when?
            // GameState = 0;

            Debug.Assert(IsWhiteTurn);
        }

        internal void MoveComplete(ChessFlags newFlags)
        {
            // The move is complete.

            var now = DateTime.UtcNow;
            if (LastMove != DateTime.MinValue && IsWhiteTurn)
            {
                WhiteTime += (now - LastMove);
            }

            MoveNumber++;   // advance turn.
            LastMove = now;

            if (FirstMove == DateTime.MinValue)
                FirstMove = now;

            GameState = newFlags;
        }
    }

    public class ChessMove
    {
        // record history of a move. Standard Notation.
        // allow to play back these moves.

        public ChessTypeId Type;     // What piece type this ?
        public ChessPosition From;    // Where did it move from?
        public ChessPosition To;    // Where did it move to?
        public ChessFlags Flags;      // Was this a Check, Castle, etc ?

        public bool IsValid()
        {
            if (Type < 0 || Type > ChessTypeId.Pawn)
                return false;
            if (!To.IsOnBoard)
                return false;
            if (ChessPiece.HasAnyFlag(Flags, ChessFlags.CheckBlock))
                return false;
            // From can be empty.
            return true;
        }

        public new string ToString()
        {
            // get notation. http://www.chesscorner.com/tutorial/basic/notation/notate.htm
            // Symbol Meaning  
            // R    Rook    
            // N    Knight  
            // B    Bishop
            // Q    Queen
            // K    King    
            // x    Captures
            // +    Check
            // ++ or #  Checkmate
            // -    moves to
            // O-O  Castles King's side	
            // O-O-O    Castles Queen's side

            var sb = new StringBuilder();

            if (ChessPiece.HasAnyFlag(Flags, ChessFlags.CastleK))
            {
                sb.Append("O-O");
            }
            else if (ChessPiece.HasAnyFlag(Flags, ChessFlags.CastleQ))
            {
                sb.Append("O-O-O");
            }
            else
            {
                if (Type != ChessTypeId.Pawn)
                {
                    sb.Append(ChessType.GetTypeLetter(Type));
                }

                sb.Append(From.Notation);

                if (ChessPiece.HasAnyFlag(Flags, ChessFlags.Capture))
                {
                    sb.Append('x');
                }
                else
                {
                    sb.Append('-');
                }

                sb.Append(To.Notation);

                if (ChessPiece.HasAnyFlag(Flags, ChessFlags.Check))
                {
                    sb.Append('+');
                }
                else if (ChessPiece.HasAnyFlag(Flags, ChessFlags.Checkmate))
                {
                    sb.Append("#");
                }
            }

            return sb.ToString();
        }

        static ChessFlags GetFlags2(string suffix, int offset)
        {
            suffix = suffix.Substring(offset);

            if (suffix.EndsWith("#") || suffix.EndsWith("++"))
            {
                return ChessFlags.Checkmate;
            }
            if (suffix.EndsWith("O-O-O"))
            {
                return ChessFlags.CastleQ;
            }
            if (suffix.EndsWith("O-O"))
            {
                return ChessFlags.CastleK;
            }
            if (suffix.EndsWith("+"))
            {
                return ChessFlags.Check;
            }

            return ChessFlags.CheckBlock;
        }

        static bool IsPos(string notation, int i)
        {
            return notation.Length >= i + 2 && ChessPosition.IsXFile(notation[i + 0]) && ChessPosition.IsYRank(notation[i + 1]);
        }

        public bool SetNotation(string notation, ChessColor color)
        {
            // Parse http://www.chesscorner.com/tutorial/basic/notation/notate.htm

            // f4 = The pawn moves to f4 // Shorthand
            // Nf3 = The Knight moves to f3. // Shorthand
            // hxg3 = The pawn on the h file takes the XXX on g3 // Shorthand
            // Bxd6 = The Bishop takes the XXX on d6 // Shorthand
            // h2xg3
            // Qxg3+        // Shorthand.
            // Bxg3#    // Shorthand.
            // O-O
            // O-O-O

            if (notation.Length < 2)
                return false;

            bool hasFrom = false;
            int i = 0;
            if (IsPos(notation, 0))
            {
                Type = ChessTypeId.Pawn; // assume its a pawn.
            }
            else if (notation[0] != 'O')
            {
                Type = ChessType.GetTypeFrom(notation[0]);      // what type moved. // may return -1;
                i = 1;
            }

            Flags = ChessFlags.OK;
            if (notation[i] == 'x')
            {
                Flags = ChessFlags.Capture;
                i++;
            }
            else if (notation[i] == '-')
            {
                i++;
            }

            if (IsPos(notation, i))
            {
                To = new ChessPosition(notation[i + 0], notation[i + 1]);
                i += 2;
            }

            if (notation.Length > i)
            {
                if (notation[i] == 'x')
                {
                    Flags = ChessFlags.Capture;
                    i++;
                }
                else if (notation[i] == '-')
                {
                    i++;
                }

                if (IsPos(notation, i))
                {
                    hasFrom = true;
                    From = To;
                    To = new ChessPosition(notation[i + 0], notation[i + 1]);
                    i += 2;
                }

                if (notation.Length > i)
                {
                    Flags |= GetFlags2(notation, i);
                }
            }

            if (!hasFrom)
            {
                if (ChessPiece.HasAnyFlag(Flags, ChessFlags.CastleK | ChessFlags.CastleQ))
                {
                    // Special King Move.
                    Type = ChessTypeId.King;
                    From = new ChessPosition(ChessPosition.kXK, color.RowKing);
                    To = From;
                }
                else if (Type == ChessTypeId.Pawn)
                {
                    // guess.
                    From = new ChessPosition(To.X, (byte)(To.Y - color.Dir)); // From may not be right?
                }
                else
                {
                    From.SetCaptured(0);    // no idea.
                }
            }

            return IsValid();
        }

        public static List<ChessMove> LoadMoves(string[] moves)
        {
            var list = new List<ChessMove>();

            foreach (string line in moves)
            {
                string[] lineMoves = line.Split((char[])null);  // split on spaces.

                if (lineMoves.Length < 1)
                    break;
                var chessMoveW = new ChessMove(lineMoves[0], ChessColor.kWhite);
                list.Add(chessMoveW);

                if (lineMoves.Length < 2)
                    break;
                var chessMoveB = new ChessMove(lineMoves[1], ChessColor.kBlack);
                list.Add(chessMoveB);
            }

            return list;
        }

        public ChessMove(string notation, ChessColor color)
        {
            // read half a line of notation. http://www.chesscorner.com/tutorial/basic/notation/notate.htm
            // e.g. "f2-f4 	e7-e5"
            SetNotation(notation, color);
        }

        public ChessMove()
        {
        }
    }

    public class ChessGame
    {
        // Store the current state of a chess game.
        // https://opensource.apple.com/source/Chess/Chess-110.0.6/Documentation/PGN-Standard.txt
        // https://www.expert-chess-strategies.com/chess-notation.html
        // https://database.chessbase.com/

        public ChessGameInfo Info = new ChessGameInfo();

        public List<ChessMove> Moves = new List<ChessMove>();    // History of moves.

        public ChessBoard Board;    // The pieces on the board.

        public void ResetGame()
        {
            // Put all pieces back to start.
            Info = new ChessGameInfo();
            Board = new ChessBoard();
            Moves = new List<ChessMove>();
        }

        public bool IsValidGame()
        {
            return Board.IsValidBoard();
        }

        internal ChessPiece GetPiece(ChessPieceId id)
        {
            return Board.GetPiece(id);
        }

        public List<ChessPosition> GetValidMovesFor(ChessPieceId id)
        {
            // List all Legal moves for a piece?
            return Board.GetValidMovesFor(id, ChessPiece.HasAnyFlag(Info.GameState, ChessFlags.Check));
        }

        private ChessFlags Move(ChessPiece piece, ChessPosition posNew)
        {
            if (piece == null)
                return ChessFlags.Invalid;
            if (piece.Color != Info.TurnColor)    // not my turn. weird.
                return ChessFlags.Invalid;
            if (ChessPiece.HasAnyFlag(Info.GameState, ChessFlags.Resigned))    // game over
                return ChessFlags.Resigned;

            ChessPosition posOld = piece.Pos;
            if (posOld.Equals(posNew))
                return ChessFlags.Invalid;

            // If this is a rook castle move. convert it to equivalent king move.
            if (piece.Type == ChessTypeId.Rook)
            {
                var color = piece.Color;
                bool isQueenSide = ChessPiece.IsQueenSideRook(piece.Id);
                byte x = isQueenSide ? ChessPosition.kXCQR : ChessPosition.kXCKR;
                if (posNew.X == x && posNew.Y == color.RowKing && Board.IsCastleable(color, isQueenSide))
                {
                    // convert Castle to the king move.
                    piece = GetPiece(color.KingId);
                    posNew.X = (byte)(ChessPosition.kXK + (isQueenSide ? -ChessOffset.kCastle : ChessOffset.kCastle));
                }
            }

            ChessFlags newFlags = Board.MoveX(piece, posNew, false, Info.IsInCheck);
            if (!ChessPiece.IsAllowedMove(newFlags))
            {
                return newFlags;
            }

            // Complete the move.
            Info.MoveComplete(newFlags);

            // Record History.
            var move = new ChessMove
            {
                Type = piece.Type,
                From = posOld,
                To = posNew,
                Flags = newFlags,
            };
            Moves.Add(move);

            return newFlags; // good.
        }

        public ChessFlags Move(ChessPieceId id, ChessPosition posNew)
        {
            // Make a move.
            // ChessFlags = result of move. (or failure)
            return Move(GetPiece(id), posNew);
        }

        public void Resign()
        {
            // Current Info.MoveColor resigns. This counts as my turn.
            Info.MoveComplete(ChessFlags.Resigned);
        }

        static int kThreadsMax = 4;

        public ChessRecommend RecommendNext(int depth = 4)
        {
            // What should my next move be ?
            return ChessRecommend.FindBest(Board, Info.TurnColor, Info.IsInCheck, depth, kThreadsMax);
        }

        public bool Move(ChessMove move)
        {
            // Play back a move where we already know the outcome.
            // RETURN: false = error. the board was not in proper state to play this move.

            if (!move.IsValid())
                return false;

            ChessFlags flags;
            ChessPiece piece = Board.GetAt(move.From);
            if (piece == null || piece.Type != move.Type)
            {
                // NOTE If the From field is not accurate we must try to guess which piece it is.
                // Find the type that can move to here.

                piece = null;
                foreach (var piece2 in Board.Pieces)
                {
                    if (piece2.Color != Info.TurnColor || piece2.Type != move.Type)
                        continue;
                    flags = Board.TestMove(piece2, move.To, Info.IsInCheck);    // can this piece make this move?
                    if (ChessPiece.IsAllowedMove(flags))
                    {
                        piece = piece2; // this is the right one.
                        break;
                    }
                }

                if (piece == null)
                    return false;
            }

            flags = Move(piece, move.To);
            return ChessPiece.IsAllowedMove(flags);     // did the move work?
        }

        public ChessGame()
        {
            // set up a new game board.
            Board = new ChessBoard();
        }
        public ChessGame(ChessPiece[] pieces)
        {
            // set up a test board.
            Board = new ChessBoard(pieces);
        }
    }
}
