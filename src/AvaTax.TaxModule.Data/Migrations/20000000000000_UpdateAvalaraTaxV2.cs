using Microsoft.EntityFrameworkCore.Migrations;

namespace AvaTax.TaxModule.Data.Migrations
{
    public partial class UpdateAvaTaxV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__MigrationHistory'))
                    BEGIN
                        UPDATE StoreTaxProvider SET [TypeName] = 'AvaTaxRateProvider' WHERE [Code] = 'AvaTaxRateProvider'
				    END");

            migrationBuilder.Sql(@"IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__MigrationHistory')) AND
                    (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlatformSetting'))
                    BEGIN
                        UPDATE [PlatformSetting] SET
                            ObjectId = [StoreTaxProvider].Id,
                            ObjectType = 'AvaTaxRateProvider'
                        FROM [PlatformSetting]
                        INNER JOIN [StoreTaxProvider] ON 
                            [StoreTaxProvider].[StoreId] = [PlatformSetting].[ObjectId] AND 
                            [StoreTaxProvider].[TypeName] = 'AvaTaxRateProvider'
                        WHERE [PlatformSetting].[Name] LIKE 'Avalara.%';
				    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //Nothing
        }
    }
}
