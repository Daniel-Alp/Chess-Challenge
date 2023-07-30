using ChessChallenge.API;
using ChessChallenge.Application;
using System;

//TODO optimize token usage

public class MyBot : IChessBot {

    int posEval, transpoFound; //debug

    public struct Transposition
    {
        public ulong hash;
        public int eval, depth, failType = 0; //Failtypes. 1 means exact eval. 2 means fail-low. 3 means fail-high.

        public Transposition(ulong h, int d, int e, int fT)
        {
            hash = h;
            depth = d;
            eval = e;
            failType = fT;
        }
    }

    Move bestMove = Move.NullMove;

    int[] pieceVal = { 0, 126, 781, 825, 1276, 2538 };
    ulong[] mgpieceSquareTable = { 114994836433535143, 214104825333364903, 206255400995132583, 171333832633604263, 125223626397159591, 149976873673234599, 190508194481725607, 202895305349329063, 220833820357785865, 187040335890872621, 165685648937958628, 180328949096852742, 179184376864766187, 183768226912769317, 145455700176451785, 155617405899528348, 178061684176445601, 215212008881228974, 190468606732218561, 170203518654980294, 165722988365475048, 195026102837879007, 213030622908891328, 163501973764787347, 169039071370442905, 165661385618022580, 174680698842704045, 157792220673700028, 154431011466457278, 160080314021163187, 172444276073096376, 147677763763106960, 133030052673775756, 187054567261908133, 157799876414069922, 144287989845119155, 136415497336065208, 138665080923547821, 151057690436759729, 130784863898991758, 172431042197733517, 172448655874357411, 163427172632153251, 136415469398836381, 138663992151435434, 154434284221030570, 171335971585065160, 157815247025101979, 189296452134119556, 196081568465406118, 179213956289817747, 116153677696372880, 139804181407111320, 170211188372858047, 198338876593562829, 197217304906453137, 171319463824652455, 228721673849948327, 201709986711188647, 127421500753057959, 197199806121269415, 156656404696018087, 215196555509125287, 203916677549801639 };
    ulong[] egpieceSquareTable = { };

    Transposition[] transpoTable = new Transposition[8388608];

    public Move Think(Board board, Timer timer)
    {
        posEval = 0;
        Search(board, timer, -10000000, 10000000, 3, 0, 0);
        Console.WriteLine("Pos eval new: " + posEval + " transpo found " + transpoFound);
        if (!board.IsWhiteToMove) Console.WriteLine();
        return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove;
    }

    public void RecordHash(ulong hash, int depth, int eval, int failType)
    {
        transpoTable[hash % 8388608] = new Transposition(hash, depth, eval, failType);
    }

    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply, int checkExtensions)
    {
        if (ply != 0 && board.IsRepeatedPosition()) return 0;
        if (board.IsInCheckmate()) return ply - 10000000;

        ulong hash = board.ZobristKey;

        Transposition transposition = transpoTable[hash % 8388608];
        if (ply != 0 && transposition.failType != 0 && hash == transposition.hash && transposition.depth >= depth)
        {
            transpoFound++;
            if (transposition.failType == 1) return transposition.eval;
            if (transposition.failType == 2) return Math.Max(alpha, transposition.eval);
            if (transposition.failType == 3) return Math.Min(beta, transposition.eval);
        }


        int bestEval = -1000000, eval = Evaluate(board), initAlpha = alpha;

        if (depth <= 0)
        {
            bestEval = eval;
            if (bestEval >= beta) return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }

        Move[] moves = board.GetLegalMoves(depth <= 0);
        Move move;

        for (int i = 0; i < moves.Length; i++)
        {
            int bestEvalGuess = -10000000, evalGuess;
            for (int j = i; j < moves.Length; j++)
            {       
                move = moves[j];
                evalGuess = (move.IsCapture ? 100 * (int)(move.CapturePieceType - move.MovePieceType) : 0) + (move.IsPromotion ? 90 : 0);
                if (evalGuess > bestEvalGuess)
                {
                (moves[i], moves[j]) = (moves[j], moves[i]);
                bestEvalGuess = evalGuess;
                }
            }

            move = moves[i];
            board.MakeMove(move);
            eval = -Search(board, timer, -beta, -alpha, depth - (board.IsInCheck() && checkExtensions < 10 ? 0 : 1), ply + 1, checkExtensions + 1);
            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                if (ply == 0) bestMove = move;
                alpha = Math.Max(alpha, bestEval);
                if (alpha >= beta) break;
            }
        }
        RecordHash(hash, depth, bestEval, bestEval >= beta ? 3 : bestEval > initAlpha ? 1 : 2);
        return bestEval;

        //int eval;

        //
        //if (ply != 0 && board.IsRepeatedPosition()) return 0;

       

        //if (depth == 0) {
        //    eval = QuietSearch(board, alpha, beta);
        //    RecordHash(hash, depth, eval, 1);
        //    return eval;
        //}

        //Move[] moves = board.GetLegalMoves();

        //int failType = 2;
        //for (int i = 0; i < moves.Length; i++)
        //{
        //    int bestEvalGuess = -10000000, evalGuess;
        //    for (int j = i; j < moves.Length; j++)
        //    {
        //        Move move = moves[j];
        //        evalGuess = (move.IsCapture ? 100 * (int)(move.CapturePieceType - move.MovePieceType) : 0) + (move.IsPromotion ? 90 : 0);
        //        if (evalGuess > bestEvalGuess)
        //        {
        //            (moves[i], moves[j]) = (moves[j], moves[i]);
        //            bestEvalGuess = evalGuess;
        //        }
        //    }

        //    board.MakeMove(moves[i]);
        //    eval = -Search(board, timer, -beta, -alpha, depth - (board.IsInCheck() && checkExtensions < 10 ? 0 : 1), ply + 1, checkExtensions + 1);
        //    board.UndoMove(moves[i]);

        //    if (eval > alpha)
        //    {
        //        failType = 1;
        //        alpha = eval;
        //        if (ply == 0) bestMove = moves[i];
        //    }
        //    if (eval >= beta)
        //    {
        //        return beta;
        //    }
        //} 
        //RecordHash(hash, depth, alpha, failType);
        //return alpha;
    }

    public ulong GetWeight(ulong bitBoard, int i, int pieceType)
    {
        return 2 * (bitBoard >> i & 1) * ((mgpieceSquareTable[i] >> (10 * pieceType)) % 512 - 167);
    }

    public int Evaluate(Board board)
    {
        posEval++;

        int eval = 0;
        for (int i = 1; i < 6; i++) eval += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceVal[i];

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