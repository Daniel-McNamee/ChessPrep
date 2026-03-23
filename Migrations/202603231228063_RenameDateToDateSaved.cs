namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameDateToDateSaved : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GameEntities", "DateSaved", c => c.DateTime(nullable: false));
            DropColumn("dbo.GameEntities", "Date");
        }
        
        public override void Down()
        {
            AddColumn("dbo.GameEntities", "Date", c => c.DateTime(nullable: false));
            DropColumn("dbo.GameEntities", "DateSaved");
        }
    }
}
