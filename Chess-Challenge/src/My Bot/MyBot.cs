using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq; //Remove?
using System.Net.NetworkInformation; //Remove?

//Not optimized for tokens, figuring out algo
public class MyBot : IChessBot
{
    struct TranspoTable
    {
        ulong hash;
        Move bestMove;
        int eval;

        public TranspoTable(ulong h, Move m, int e)
        {
            hash = h;
            bestMove = m;
            eval = e;
        }
    }

    Move bestMove = Move.NullMove;
    
    //
    int[] pieceVal = {0, 126, 781, 825, 1276, 2538};
    
    //Table generated with the help of a Java program and values based on PeSTO
    //See https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function and https://github.com/Daniel-Alp/Piece-Square-Table-Generator
    ulong[] mgpieceSquareTable = {114994836433535143, 214104825333364903, 206255400995132583, 171333832633604263, 125223626397159591, 149976873673234599, 190508194481725607, 202895305349329063, 220833820357785865, 187040335890872621, 165685648937958628, 180328949096852742, 179184376864766187, 183768226912769317, 145455700176451785, 155617405899528348, 178061684176445601, 215212008881228974, 190468606732218561, 170203518654980294, 165722988365475048, 195026102837879007, 213030622908891328, 163501973764787347, 169039071370442905, 165661385618022580, 174680698842704045, 157792220673700028, 154431011466457278, 160080314021163187, 172444276073096376, 147677763763106960, 133030052673775756, 187054567261908133, 157799876414069922, 144287989845119155, 136415497336065208, 138665080923547821, 151057690436759729, 130784863898991758, 172431042197733517, 172448655874357411, 163427172632153251, 136415469398836381, 138663992151435434, 154434284221030570, 171335971585065160, 157815247025101979, 189296452134119556, 196081568465406118, 179213956289817747, 116153677696372880, 139804181407111320, 170211188372858047, 198338876593562829, 197217304906453137, 171319463824652455, 228721673849948327, 201709986711188647, 127421500753057959, 197199806121269415, 156656404696018087, 215196555509125287, 203916677549801639};
    
    int posEvaluated;

    public Move Think(Board board, Timer timer)
    {
        Search(board, timer, -10000000, 10000000, 3, 0, 0);
        return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove;
    }

    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int layer, int checkExtensions)
    {
        //Favour checkmates that will require more moves
        if (board.IsInCheckmate()) return layer - 10000000;

        //Quiescence search to avoid horizon effect
        if (depth == 0) return QuietSearch(board, alpha, beta);

        Move[] moves = board.GetLegalMoves();

        //If not in checkmate and move count is 0, must be draw
        if (moves.Length == 0) return 0;


        for (int i = 0; i < moves.Length; i++)
        {
            //Selection sort for likely best move
            int likelyBestEval = -10000000, likelyCurEval;
            for (int j = i; j < moves.Length; j++)
            {
                Move move = moves[j];
                //Favour stronger pieces captured by weaker pieces and promotions
                likelyCurEval = (move.IsCapture ? 100 * (int)(move.CapturePieceType - move.MovePieceType) : 0) + (move.IsPromotion ? 90 : 0);
                if (likelyCurEval > likelyBestEval)
                {
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                    likelyBestEval = likelyCurEval;
                }
            }

            board.MakeMove(moves[i]);
            int eval = -Search(board, timer, -beta, -alpha, depth - (board.IsInCheck() && checkExtensions < 10 ? 0 : 1), layer + 1, checkExtensions + 1);
            board.UndoMove(moves[i]);
            if (eval > alpha)
            {
                alpha = eval;
                if (layer == 0) bestMove = moves[i];
                if (alpha >= beta) break;
            }
        }

        return alpha;
    }

    public int QuietSearch(Board board, int alpha, int beta)
    {
        int eval = Evaluate(board);
        if (eval >= beta) return beta;
        alpha = Math.Max(eval, alpha);
        foreach (Move move in board.GetLegalMoves(true))
        {
            board.MakeMove(move);
            eval = -QuietSearch(board, -beta, -alpha);
            board.UndoMove(move);
            alpha = Math.Max(eval, alpha);
            if (eval >= beta) return beta;
        }
        return alpha;
    }

    public ulong GetWeight(ulong bitBoard, int i, int pieceType)
    {
        return 2 * (bitBoard >> i & 1) * ((mgpieceSquareTable[i] >> (10 * pieceType)) % 512 - 167);
    }

    //My goal is to use the most important heuristics in the last tokens (obviously! :D) 
    public int Evaluate(Board board)
    {
        posEvaluated += 1;

        int eval = 0;
        //Material score
        for (int i = 1; i < 6; i++) eval += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceVal[i];

        //Piece square table score
        ulong bitBoard;
        for (int i = 0; i < 12; i++)
        {
            bitBoard = board.GetPieceBitboard((PieceType)(i % 6 + 1), i < 6);
            for (int r = 0; r < 8; r++)
            {
               for (int c = 0; c < 8; c++)
               {
                    if (i < 6) eval += (int)GetWeight(bitBoard, r * 8 + c, i % 6);
                    else eval -= (int)GetWeight(bitBoard, (7 - r) * 8 + c, i % 6);
               }
            }
        }

        return eval * (board.IsWhiteToMove ? 1 : -1);
    }
}