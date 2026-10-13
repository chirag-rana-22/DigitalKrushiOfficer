using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFarmerCropCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CropId",
                table: "TblFarmerCrop",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CustomCropName",
                table: "TblFarmerCrop",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Insert by name rather than fixed IDs, preserving crops already maintained by administrators.
            // Seasons are intentionally unspecified: planting windows depend on region and variety.
            string[] crops = [
                "Maize", "Jowar (Sorghum)", "Ragi (Finger millet)", "Barley", "Oats",
                "Foxtail millet", "Little millet", "Kodo millet", "Barnyard millet", "Proso millet",
                "Chickpea (Gram)", "Pigeon pea (Tur)", "Green gram (Moong)", "Black gram (Urad)",
                "Lentil (Masoor)", "Cowpea", "Field pea", "Kidney bean (Rajma)", "Horse gram",
                "Soybean", "Mustard", "Sesame (Til)", "Sunflower", "Safflower", "Linseed",
                "Tomato", "Onion", "Potato", "Brinjal (Eggplant)", "Okra (Bhindi)",
                "Cabbage", "Cauliflower", "Chilli", "Capsicum", "Carrot", "Radish", "Beetroot",
                "Cucumber", "Bottle gourd", "Bitter gourd", "Ridge gourd", "Pumpkin", "Ash gourd",
                "French bean", "Cluster bean (Guar)", "Garden pea", "Spinach", "Amaranth",
                "Garlic", "Ginger", "Coriander", "Cumin", "Fennel", "Fenugreek (Methi)", "Ajwain",
                "Mango", "Banana", "Papaya", "Guava", "Pomegranate", "Grapes", "Lemon", "Sweet lime",
                "Orange", "Sapota (Chikoo)", "Watermelon", "Muskmelon", "Strawberry", "Custard apple",
                "Dragon fruit", "Coconut", "Date palm", "Lucerne (Alfalfa)", "Berseem", "Napier grass",
                "Marigold", "Rose", "Jasmine", "Chrysanthemum", "Jute"
            ];
            foreach (var name in crops)
                migrationBuilder.Sql($"INSERT INTO `TblCrop` (`CropName`, `Season`, `Description`, `IsActive`, `CreatedDate`) SELECT '{name}', NULL, NULL, 1, '2026-10-13 00:00:00' WHERE NOT EXISTS (SELECT 1 FROM `TblCrop` WHERE LOWER(TRIM(`CropName`)) = LOWER('{name}'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration preserves farmer-entered crop names. Use a reviewed forward migration instead of deleting these records during rollback.");
        }
    }
}
