using ChessChallenge.API;
using System;

//TODO optimize token usage

public class MyBot : IChessBot
{
    public struct Transposition
    {
        public ulong hash;
        public int eval, depth, failType = 0; //Failtypes. 1 means exact eval. 2 means fail-low. 3 means fail-high.
        public Move bestMove;

        public Transposition(ulong h, int d, int e, Move bM, int fT)
        {
            hash = h;
            depth = d;
            eval = e;
            bestMove = bM;
            failType = fT;
        }
    }

    Move bestMove;

    int[] pieceVal = { 0, 82, 337, 365, 477, 1025, 0 }, phaseVal = { 1, 1, 2, 2, 4, 0 };
    ulong[] mgpieceSquareTable = { 6004234345560363859, 5216961999590216514, 5576676063466049862, 5140953929278902854, 5214140734727674444, 5287348508207109968, 5648800866043598468, 6004234345560363859, 5209052108559894815, 5353755569969510725, 5431442706592780104, 5719111249516254541, 6799439719732960335, 7599987637732405564, 5428933629868195631, 2183158892895282944, 5278303757615780419, 6081089126010674005, 6367071013902703443, 6149763578822548048, 5933323624174671441, 5937552341517035083, 4349476007150508870, 5717106727727027525, 5062423506043817290, 3481380726304754493, 4850749899455285053, 5212441959012845121, 5282552348241514055, 6589446149866806609, 7593198144808051553, 7594010649456502883, 4198559145840822867, 6076019277911641922, 6222378543415710796, 5932737498073482831, 6076020360091485766, 8100690822918785869, 7953761885752475719, 7667775776126817093, 6511999827110880588, 6293863209771161428, 5065498710680030284, 4198266606625116987, 4705214069258144075, 5214700291338035023, 4990077788217758562, 6508900166728900403 };
    ulong[] egpieceSquareTable = { 6004234345560363859, 5788343068572211034, 5716001770401978197, 6004792884531976282, 6655288161471192931, 9042223576844764546, 12801083494916860588, 6004234345560363859, 3691344518961445445, 4415860052537002302, 5208784983473476168, 5356288879355449418, 5356848543625532747, 4560544792049109319, 4127345989516742471, 2464672155112914998, 5427201829996351304, 5065510908773550668, 5498988991536910925, 5715728009338180944, 6076860417258903634, 6148352827876134740, 5499261635876311375, 5137287006292691276, 5284214749474935887, 5930764961429278800, 5426641083395298129, 5642817168372880981, 6076293043422123349, 5931612710697916247, 6149477628372474457, 6221539650555369562, 4560250113905673539, 4846234162249877576, 6221824419688040011, 6874573734370173258, 7309455927740948053, 6367356960225384009, 6008470888769609035, 6726228716304621135, 4487639414247866937, 5427212872509115974, 5717138717133066826, 5645367004725399882, 6152027456101375567, 6514855285165612120, 6439978539035548749, 5428345317676958254};

    Transposition[] transpoTable = new Transposition[67108864];

    public Move Think(Board board, Timer timer)
    {
        bestMove = Move.NullMove;
        for (int i = 1; i < 40; i++)
        {
            Search(board, timer, -10000000, 10000000, i, 0, 0);
            if (timer.MillisecondsElapsedThisTurn * 30 >= timer.MillisecondsRemaining) break;
        }
        return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove;
    }

    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply, int checkExtensions)
    {
        if (ply != 0 && board.IsRepeatedPosition()) return 0;

        ulong hash = board.ZobristKey;

        Transposition transposition = transpoTable[hash % 67108864];
        if (ply != 0 && transposition.failType != 0 && hash == transposition.hash && transposition.depth >= depth)
        {
            if (transposition.failType == 1) return transposition.eval;
            if (transposition.failType == 2 && transposition.eval >= beta) return beta;
            if (transposition.failType == 3 && transposition.eval <= alpha) return alpha;
        }


        int bestEval = -1000000, eval = Evaluate(board), initAlpha = alpha;

        if (depth <= 0)
        {
            bestEval = eval;
            if (bestEval >= beta) return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }

        Move[] moves = board.GetLegalMoves(depth <= 0);
        Move move, bestMoveIter = Move.NullMove;
        if (depth > 0 && moves.Length == 0) return board.IsInCheck() ? -10000000 : 0;

        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn * 30 >= timer.MillisecondsRemaining) return 10000000;
            int bestEvalGuess = -10000000, evalGuess;
            for (int j = i; j < moves.Length; j++)
            {
                move = moves[j];
                evalGuess = (move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType : 0) + (move.IsPromotion ? 90 : 0) + (transposition.bestMove == move ? 1000000 : 0);
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
                bestMoveIter = move;
                if (ply == 0) bestMove = move;
                alpha = Math.Max(alpha, bestEval);
                if (alpha >= beta) break;
            }
        }

        transpoTable[hash % 67108864] = new Transposition(hash, depth, eval, bestMoveIter, bestEval >= beta ? 3 : bestEval > initAlpha ? 1 : 2);
        return bestEval;
    }

    public int GetSquareTableEval(int pieceType, int index, ulong[] squareTable)
    {
        return (int)((squareTable[pieceType * 8 + (index >> 3)] >> (index % 8 * 8)) % 256) * 2 - 167;
    }

    public int Evaluate(Board board)
    {
        int materialEval = 0, midgameEval = 0, endgameEval = 0, phase = 0;
        for (int i = 1; i < 6; i++) materialEval += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceVal[i];

        ulong bitBoard;
        for (int i = 0; i < 12; i++)
        {
            bitBoard = board.GetPieceBitboard((PieceType)(i % 6 + 1), i < 6);
            for (int j = 0; j < 64; j++)
            {
                if ((bitBoard & (1UL << j)) != 0)
                {
                    phase += phaseVal[i % 6];
                    if (i < 6) {
                        midgameEval += GetSquareTableEval(i % 6, j, mgpieceSquareTable);
                        endgameEval += GetSquareTableEval(i % 6, j, egpieceSquareTable);
                    }
                    else {
                        midgameEval -= GetSquareTableEval(i % 6, j ^ 56, mgpieceSquareTable);
                        endgameEval -= GetSquareTableEval(i % 6, j ^ 56, egpieceSquareTable);
                    }
                }
            }
        }
        phase = Math.Max(phase, 24);
        return (materialEval + (midgameEval * phase + endgameEval * (phase - 24)) / 24) * (board.IsWhiteToMove ? 1 : -1);
    }
}