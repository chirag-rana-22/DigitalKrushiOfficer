-- Safe catalog import. Source: legacy project SQL crop INSERT only.
-- Run with the application database selected. No tables or farmer records are deleted.
START TRANSACTION;
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Cotton', 'Kharif', 'Major cash crop of Gujarat', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Cotton'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Groundnut', 'Kharif', 'Oilseed crop suitable for Gujarat soil', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Groundnut'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Wheat', 'Rabi', 'Winter cereal crop', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Wheat'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Bajra', 'Kharif', 'Millet crop drought resistant', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Bajra'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Turmeric', 'Kharif', 'Spice crop with medicinal value', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Turmeric'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Rice', 'Kharif', 'Staple food crop', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Rice'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Sugarcane', 'Year-round', 'Cash crop for sugar production', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Sugarcane'));
INSERT INTO TblCrop (CropName, Season, Description, IsActive, CreatedDate) SELECT 'Castor', 'Kharif', 'Oilseed industrial crop', 1, NOW() WHERE NOT EXISTS (SELECT 1 FROM TblCrop WHERE LOWER(TRIM(CropName)) = LOWER('Castor'));
COMMIT;
SELECT Id, CropName, Season, IsActive FROM TblCrop ORDER BY CropName;
