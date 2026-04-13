using ChessProject.Entities;
using ChessProject.Models;
using System;

namespace ChessProject.Services
{
    public static class GameMapper
    {
        // Maps a ChessGame model to a GameEntity for database storage
        public static GameEntity ToEntity(ChessGame game)
        {
            return new GameEntity
            {
                WhitePlayer = game.White,
                BlackPlayer = game.Black,
                PerspectivePlayer = game.PerspectivePlayer ?? "",
                WhiteElo = game.WhiteElo,
                BlackElo = game.BlackElo,
                Result = game.Result,
                PGN = game.Pgn,
                TimeControl = game.GameType,
                DateSaved = DateTime.Now,
                IsFavourite = false
            };
        }

        // Maps a ChessGame model to a RecentGameEntity for recent games list
        public static RecentGameEntity ToRecentEntity(ChessGame game)
        {
            return new RecentGameEntity
            {
                WhitePlayer = game.White,
                BlackPlayer = game.Black,
                PerspectivePlayer = game.PerspectivePlayer ?? "",
                WhiteElo = game.WhiteElo,
                BlackElo = game.BlackElo,
                Result = game.Result,
                PGN = game.Pgn,
                TimeControl = game.GameType,
                DateViewed = DateTime.Now
            };
        }

        // Maps a GameEntity from the database to a ChessGame model for use in the application
        public static ChessGame ToModel(GameEntity entity)
        {
            return new ChessGame
            {
                White = entity.WhitePlayer,
                Black = entity.BlackPlayer,
                PerspectivePlayer = entity.PerspectivePlayer,
                WhiteElo = entity.WhiteElo,
                BlackElo = entity.BlackElo,
                Result = entity.Result,
                Pgn = entity.PGN,
                GameType = entity.TimeControl,
                HasNotes = entity.HasNotes
            };
        }

        // Maps a RecentGameEntity to a ChessGame model, which can be used when displaying recent games
        public static ChessGame ToModel(RecentGameEntity entity)
        {
            return new ChessGame
            {
                White = entity.WhitePlayer,
                Black = entity.BlackPlayer,
                PerspectivePlayer = entity.PerspectivePlayer,
                WhiteElo = entity.WhiteElo,
                BlackElo = entity.BlackElo,
                Result = entity.Result,
                Pgn = entity.PGN,
                GameType = entity.TimeControl
            };
        }

        // Maps a ChessGame model to a LocalGameEntity for local game storage
        public static LocalGameEntity ToLocalEntity(ChessGame game)
        {
            return new LocalGameEntity
            {
                WhitePlayer = game.White,
                BlackPlayer = game.Black,
                Result = game.Result,
                PGN = game.Pgn,
                DatePlayed = DateTime.Now
            };
        }

        // Maps a LocalGameEntity to a ChessGame model, which can be used when displaying local games
        public static ChessGame ToModel(LocalGameEntity entity)
        {
            return new ChessGame
            {
                White = entity.WhitePlayer,
                Black = entity.BlackPlayer,
                PerspectivePlayer = "",
                WhiteElo = 0, // local games won’t have rating
                BlackElo = 0,
                Result = entity.Result,
                Pgn = entity.PGN,
                GameType = "Local Game",
                HasNotes = false // Might change later
            };
        }

    }
}