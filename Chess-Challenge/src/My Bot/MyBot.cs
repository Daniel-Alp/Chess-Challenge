using ChessChallenge.API;
using System;
using System.Linq;
using System.Net.NetworkInformation;

public class MyBot : IChessBot {
    Move bestMove = Move.NullMove;
    
    public Move Think(Board board, Timer timer) 
    {
        Search(board, timer, 5, 1);
        if (bestMove == Move.NullMove)
        {
            Console.WriteLine("Oopsy!");
            return board.GetLegalMoves()[0];
        } else
        {
            return bestMove;
        }
    }
    
    public int Search(Board board, Timer timer, int depth, int colour) 
    {
        if (depth == 0) 
        {
            return colour * Evaluate(board);
        }
        Move[] moves = board.GetLegalMoves();
        int eval, bestEval = -10000;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            eval = Search(board, timer, depth - 1, -colour);
            board.UndoMove(move);
            if (eval > bestEval)
            {
                bestEval = eval;
                if (depth - 5 == 0)
                {
                    Console.WriteLine("New best move found!");
                    bestMove = move;
                }
            }
        }
        return bestEval;
    }

    public int Evaluate(Board board) 
    {
        if (board.IsInCheckmate())
        {
            return -10000;
        }
        return 0;
    }
}