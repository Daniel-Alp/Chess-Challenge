using ChessChallenge.API;
using System;

//TODO optimize token usage

public class MyBot : IChessBot
{

    public struct Transposition
    {
        public ulong hash;
        public int eval, depth, failType = 0;
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

    Board board;
    Timer timer;
    Move bestMove;
    ulong[] mgpieceSquareTable = { 6004234345560363859, 5216961999590216514, 5576676063466049862, 5140953929278902854, 5214140734727674444, 5287348508207109968, 5648800866043598468, 6004234345560363859, 5209052108559894815, 5353755569969510725, 5431442706592780104, 5719111249516254541, 6799439719732960335, 7599987637732405564, 5428933629868195631, 2183158892895282944, 5278303757615780419, 6081089126010674005, 6367071013902703443, 6149763578822548048, 5933323624174671441, 5937552341517035083, 4349476007150508870, 5717106727727027525, 5062423506043817290, 3481380726304754493, 4850749899455285053, 5212441959012845121, 5282552348241514055, 6589446149866806609, 7593198144808051553, 7594010649456502883, 4198559145840822867, 6076019277911641922, 6222378543415710796, 5932737498073482831, 6076020360091485766, 8100690822918785869, 7953761885752475719, 7667775776126817093, 6511999827110880588, 6293863209771161428, 5065498710680030284, 4198266606625116987, 4705214069258144075, 5214700291338035023, 4990077788217758562, 6508900166728900403 }, egpieceSquareTable = { 6004234345560363859, 5788343068572211034, 5716001770401978197, 6004792884531976282, 6655288161471192931, 9042223576844764546, 12801083494916860588, 6004234345560363859, 3691344518961445445, 4415860052537002302, 5208784983473476168, 5356288879355449418, 5356848543625532747, 4560544792049109319, 4127345989516742471, 2464672155112914998, 5427201829996351304, 5065510908773550668, 5498988991536910925, 5715728009338180944, 6076860417258903634, 6148352827876134740, 5499261635876311375, 5137287006292691276, 5284214749474935887, 5930764961429278800, 5426641083395298129, 5642817168372880981, 6076293043422123349, 5931612710697916247, 6149477628372474457, 6221539650555369562, 4560250113905673539, 4846234162249877576, 6221824419688040011, 6874573734370173258, 7309455927740948053, 6367356960225384009, 6008470888769609035, 6726228716304621135, 4487639414247866937, 5427212872509115974, 5717138717133066826, 5645367004725399882, 6152027456101375567, 6514855285165612120, 6439978539035548749, 5428345317676958254 };
    int[] pieceVal = { 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0 }, phaseVal = { 1, 1, 2, 2, 4, 0 }, pieceSquareTable = new int[768];
    bool abortSearch;
    Transposition[] transpoTable = new Transposition[1048576];

    int Search(int alpha, int beta, int depth, int ply)
    {
        if (timer.MillisecondsElapsedThisTurn * 30 >= timer.MillisecondsRemaining)
        {
            abortSearch = true;
            return 0;
        };

        if (ply != 0 && board.IsRepeatedPosition()) return 0;

        ulong hash = board.ZobristKey;
        Transposition transposition = transpoTable[hash % 1048576];
        int failType = transposition.failType, transpositionEval = transposition.eval;
        if (ply != 0 && transposition.failType != 0 && hash == transposition.hash && transposition.depth >= depth)
        {
            if (failType == 1) return transpositionEval;
            if (failType == 2 && transpositionEval >= beta) return beta;
            if (failType == 3 && transpositionEval <= alpha) return alpha;
        }

        int bestEval = -1000000, eval = Evaluate(), initAlpha = alpha;

        if (depth <= 0 && !board.IsInCheck())
        {
            bestEval = eval;
            if (bestEval >= beta) return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }

        Move[] moves = board.GetLegalMoves(depth <= 0);
        Move move, bestMoveIter = Move.NullMove;
        if (depth > 0 && moves.Length == 0) return board.IsInCheck() ? ply - 10000000 : 0;

        for (int i = -1; ++i < moves.Length;)
        {
            int bestEvalGuess = -10000000, evalGuess;
            for (int j = i; j < moves.Length; j++)
            {
                move = moves[j];
                evalGuess = 0;
                if (move.IsCapture) evalGuess += 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
                if (move.IsPromotion) evalGuess += 936;
                if (move == transposition.bestMove) evalGuess += 1000000;

                if (evalGuess > bestEvalGuess)
                {
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                    bestEvalGuess = evalGuess;
                }
            }

            move = moves[i];
            board.MakeMove(move);
            eval = -Search(-beta, -alpha, depth - (board.IsInCheck() ? 0 : 1), ply + 1);
            board.UndoMove(move);
            if (abortSearch) return 0;
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMoveIter = move;
                if (ply == 0) bestMove = move;
                alpha = Math.Max(alpha, bestEval);
                if (alpha >= beta) break;
            }
        }

        transpoTable[hash % 1048576] = new Transposition(hash, depth, bestEval, bestMoveIter, bestEval >= beta ? 2 : bestEval > initAlpha ? 1 : 3);
        return bestEval;
    }

    int GetSquareTableEval(int pieceType, int index, ulong[] squareTable)
    {
        return (int)((squareTable[index / 8 + pieceType * 8] >> (index % 8 * 8)) % 256) * 2 - 167;
    }

    int Evaluate()
    {
        int midgameEval = 0, endgameEval = 0, phase = 0;
        foreach (bool isWhite in new[]{ true, false })
        {
            for (int pieceType = -1; ++pieceType < 6;)
            {
                ulong bitboard = board.GetPieceBitboard((PieceType)(pieceType + 1), isWhite);
                while (bitboard != 0)
                {
                    phase += phaseVal[pieceType];
                    int i = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ (isWhite ? 0 : 56);
                    midgameEval += pieceSquareTable[pieceType * 64 + i] + pieceVal[pieceType];
                    endgameEval += pieceSquareTable[pieceType * 64 + i + 384] + pieceVal[pieceType + 6];
                }
            }
            midgameEval *= -1;
            endgameEval *= -1;
        }
        phase = Math.Min(phase, 24);
        return (midgameEval * phase + endgameEval * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;
        abortSearch = false;
        bestMove = Move.NullMove;
        for (int i = 0; ++i < 40;)
        {
            Search(-10000000, 10000000, i, 0);
            if (abortSearch) break;
        }
        return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove;
    }

    public MyBot()
    {
        for (int i = -1; ++i < 384;)
        {
            pieceSquareTable[i] = GetSquareTableEval(i >> 6, i % 64, mgpieceSquareTable);
            pieceSquareTable[i + 384] = GetSquareTableEval(i >> 6, i % 64, egpieceSquareTable);
        }
    }
}