namespace ChessProject.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddHasNotes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GameEntities", "HasNotes", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GameEntities", "HasNotes");
        }
    }
}
