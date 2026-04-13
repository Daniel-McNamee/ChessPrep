namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateLocalGameEntity : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.LocalGameEntities", "WhitePlayer", c => c.String());
            AddColumn("dbo.LocalGameEntities", "BlackPlayer", c => c.String());
            AddColumn("dbo.LocalGameEntities", "PGN", c => c.String());
            DropColumn("dbo.LocalGameEntities", "Moves");
        }
        
        public override void Down()
        {
            AddColumn("dbo.LocalGameEntities", "Moves", c => c.String());
            DropColumn("dbo.LocalGameEntities", "PGN");
            DropColumn("dbo.LocalGameEntities", "BlackPlayer");
            DropColumn("dbo.LocalGameEntities", "WhitePlayer");
        }
    }
}
