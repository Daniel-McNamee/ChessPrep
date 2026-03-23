using ChessProject.Entities;
using ChessProject.Models;
using System;

namespace ChessProject.Services
{
    public static class GameMapper
    {
        public static GameEntity ToEntity(ChessGame game)
        {
            return new GameEntity
            {
                WhitePlayer = game.White,
                BlackPlayer = game.Black,
                WhiteElo = game.WhiteElo,
                BlackElo = game.BlackElo,
                Result = game.Result,
                PGN = game.Pgn,
                TimeControl = game.GameType,
                DateSaved = DateTime.Now,
                IsFavourite = false
            };
        }

        public static RecentGameEntity ToRecentEntity(ChessGame game)
        {
            return new RecentGameEntity
            {
                WhitePlayer = game.White,
                BlackPlayer = game.Black,
                WhiteElo = game.WhiteElo,
                BlackElo = game.BlackElo,
                Result = game.Result,
                PGN = game.Pgn,
                TimeControl = game.GameType,
                DateViewed = DateTime.Now
            };
        }

        public static ChessGame ToModel(GameEntity entity)
        {
            return new ChessGame
            {
                White = entity.WhitePlayer,
                Black = entity.BlackPlayer,
                WhiteElo = entity.WhiteElo,
                BlackElo = entity.BlackElo,
                Result = entity.Result,
                Pgn = entity.PGN,
                GameType = entity.TimeControl
            };
        }

        public static ChessGame ToModel(RecentGameEntity entity)
        {
            return new ChessGame
            {
                White = entity.WhitePlayer,
                Black = entity.BlackPlayer,
                WhiteElo = entity.WhiteElo,
                BlackElo = entity.BlackElo,
                Result = entity.Result,
                Pgn = entity.PGN,
                GameType = entity.TimeControl
            };
        }
    }
}