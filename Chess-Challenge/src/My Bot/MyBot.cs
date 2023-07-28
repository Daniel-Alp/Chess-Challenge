using ChessChallenge.API;
using System;
using System.Linq; //Remove?
using System.Net.NetworkInformation; //Remove?

public class MyBot : IChessBot {
    Move bestMove = Move.NullMove;
    
    public Move Think(Board board, Timer timer) 
    {
        Search(board, timer, -10000, 10000, 5, 0);
        if (bestMove == Move.NullMove)
        {
            Console.WriteLine("Oopsy!");
            return board.GetLegalMoves()[0];
        } else
        {
            return bestMove;
        }
    }
    
    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int layer) 
    {
        if (board.IsInCheckmate())
        {
            return -10000 + layer; //Favour checkmates that will require more moves
        }

        if (depth == 0)
        {
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0)
        {
            return 0;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(board, timer, -beta, -alpha, depth - 1, layer + 1);
            board.UndoMove(move);

            if (eval > alpha)
            {
                alpha = eval;
                if (layer == 0)
                {
                    bestMove = move;
                }
                if (alpha >= beta)
                {
                    break;
                }
            }
        }

        return alpha;
    }

    public int Evaluate(Board board) 
    {
        return 0;
    }
}