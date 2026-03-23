namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RmIsFavouriteAddDateAdded : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OpeningEntities", "DateAdded", c => c.DateTime(nullable: false));
            DropColumn("dbo.OpeningEntities", "IsFavourite");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OpeningEntities", "IsFavourite", c => c.Boolean(nullable: false));
            DropColumn("dbo.OpeningEntities", "DateAdded");
        }
    }
}
