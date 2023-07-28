using ChessChallenge.API;
using System;
using System.Linq; //Remove?
using System.Net.NetworkInformation; //Remove?

//Not optimized for tokens, figuring out algo
public class MyBot : IChessBot {
    Move bestMove = Move.NullMove;
    int[] pieceVal = {0, 10, 30, 35, 50, 90};
    int posEvaluated;
    
    public Move Think(Board board, Timer timer) 
    {
        posEvaluated = 0;
        Search(board, timer, -10000000, 10000000, 7, 0);
        if (bestMove == Move.NullMove)
        {
            Console.WriteLine("Oopsy!");
            return board.GetLegalMoves()[0];
        } else
        {
            Console.WriteLine("Pos Evaluated " + posEvaluated);
            return bestMove;
        }
    }

    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int layer)
    {
        //Favour checkmates that will require more moves
        if (board.IsInCheckmate()) return layer  - 10000000;

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
                likelyCurEval = (move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType : 0) + (move.IsPromotion ? 90 : 0);
                if (likelyCurEval > likelyBestEval)
                {
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                    likelyBestEval = likelyCurEval;
                }
            }

            board.MakeMove(moves[i]);
            int eval = -Search(board, timer, -beta, -alpha, depth - 1, layer + 1);
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

    public int Evaluate(Board board) 
    {
        posEvaluated += 1;
        int eval = 0;
        for (int i = 1; i < 6; i++) eval += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceVal[i];
        return eval * (board.IsWhiteToMove ? 1 : -1);
    }
}