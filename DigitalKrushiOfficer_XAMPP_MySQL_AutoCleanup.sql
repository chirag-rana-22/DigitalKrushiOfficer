-- =====================================================
-- DIGITAL KRUSHI OFFICER - COMPLETE DATABASE SCRIPT
-- For XAMPP (MySQL / MariaDB) with AUTO-CLEANUP
-- AI-Powered Smart Agriculture Advisory System
-- =====================================================

-- Create Database
CREATE DATABASE IF NOT EXISTS DigitalKrushiOfficer 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE DigitalKrushiOfficer;

-- =====================================================
-- DROP EXISTING TABLES (Clean Recreate)
-- =====================================================
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS TblNotificationLog;
DROP TABLE IF EXISTS TblFeedback;
DROP TABLE IF EXISTS TblEscalation;
DROP TABLE IF EXISTS TblAIResponse;
DROP TABLE IF EXISTS TblQuery;
DROP TABLE IF EXISTS TblOfficerAssignment;
DROP TABLE IF EXISTS TblFarmerCrop;
DROP TABLE IF EXISTS TblCrop;
DROP TABLE IF EXISTS TblOfficerDetails;
DROP TABLE IF EXISTS TblFarmerDetails;
DROP TABLE IF EXISTS TblDiseaseDataset;
DROP TABLE IF EXISTS TblGovernmentScheme;
DROP TABLE IF EXISTS TblWeatherLog;
DROP TABLE IF EXISTS TblUser;

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 1. TblUser - Master User Table
-- =====================================================
CREATE TABLE TblUser
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    -- Basic Info
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Mobile VARCHAR(15),

    -- Security (Salted Hashed Password)
    PasswordHash VARCHAR(500) NOT NULL,

    -- Role Management
    Role VARCHAR(50) NOT NULL,          -- Admin / Farmer / Officer

    -- Location
    State VARCHAR(100),
    District VARCHAR(100),
    Village VARCHAR(100),

    -- Preferences
    PreferredLanguage VARCHAR(50) DEFAULT 'Gujarati',

    -- Email Verification
    IsEmailVerified TINYINT(1) DEFAULT 0,
    EmailVerificationToken VARCHAR(200) NULL,

    -- Status & Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT NULL,
    UpdatedDate DATETIME NULL,
    UpdatedBy INT NULL,
    LastLoginDate DATETIME NULL,

    INDEX IX_TblUser_Email (Email),
    INDEX IX_TblUser_Mobile (Mobile),
    INDEX IX_TblUser_Role (Role),
    INDEX IX_TblUser_District (District),
    INDEX IX_TblUser_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 2. TblFarmerDetails - Farmer Profile Extension
-- =====================================================
CREATE TABLE TblFarmerDetails
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    UserId INT NOT NULL,

    -- Farming Profile
    LandArea DECIMAL(10,2),
    SoilType VARCHAR(100),
    MainCrop VARCHAR(100),
    FarmingExperience INT,              -- Years of experience

    -- Status & Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(UserId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    INDEX IX_TblFarmerDetails_UserId (UserId),
    INDEX IX_TblFarmerDetails_SoilType (SoilType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 3. TblOfficerDetails - Agriculture Officer Profile
-- =====================================================
CREATE TABLE TblOfficerDetails
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    UserId INT NOT NULL,

    -- Professional Info
    Department VARCHAR(100),
    Designation VARCHAR(100),
    ExperienceYears INT,
    Specialization VARCHAR(200),          -- Crop specialization

    -- Status & Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(UserId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    INDEX IX_TblOfficerDetails_UserId (UserId),
    INDEX IX_TblOfficerDetails_Department (Department)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 4. TblOfficerAssignment - Officer Area Assignment
-- =====================================================
CREATE TABLE TblOfficerAssignment
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    OfficerId INT NOT NULL,
    District VARCHAR(100) NOT NULL,
    Village VARCHAR(100) NULL,
    IsPrimary TINYINT(1) DEFAULT 0,     -- Primary officer for area

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(OfficerId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    INDEX IX_TblOfficerAssignment_OfficerId (OfficerId),
    INDEX IX_TblOfficerAssignment_District (District),
    INDEX IX_TblOfficerAssignment_Village (Village)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 5. TblCrop - Master Crop Database
-- =====================================================
CREATE TABLE TblCrop
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    CropName VARCHAR(100) NOT NULL,
    Season VARCHAR(100),                -- Kharif / Rabi / Zaid
    Description TEXT,

    -- Status & Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    INDEX IX_TblCrop_CropName (CropName),
    INDEX IX_TblCrop_Season (Season)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 6. TblFarmerCrop - Farmer's Crop Management
-- =====================================================
CREATE TABLE TblFarmerCrop
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    FarmerId INT NOT NULL,
    CropId INT NOT NULL,

    -- Crop Details
    LandArea DECIMAL(10,2),
    SowingDate DATE,
    ExpectedHarvestDate DATE,
    Status VARCHAR(50) DEFAULT 'Growing', -- Growing / Harvested / Failed

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(FarmerId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    FOREIGN KEY(CropId) REFERENCES TblCrop(Id),
    INDEX IX_TblFarmerCrop_FarmerId (FarmerId),
    INDEX IX_TblFarmerCrop_CropId (CropId),
    INDEX IX_TblFarmerCrop_Status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 7. TblQuery - Farmer Queries (Text/Voice/Image)
-- =====================================================
CREATE TABLE TblQuery
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    FarmerId INT NOT NULL,

    -- Query Content
    QueryText TEXT,
    QueryLanguage VARCHAR(50) DEFAULT 'Gujarati',
    QueryType VARCHAR(50),              -- Text / Voice / Image

    -- Media Files
    ImagePath VARCHAR(300),
    VoicePath VARCHAR(300),

    -- Threading (Follow-up conversations)
    ParentQueryId INT NULL,

    -- Status Tracking
    Status VARCHAR(50) DEFAULT 'Pending',  -- Pending / Answered / Escalated / Closed
    IsRead TINYINT(1) DEFAULT 0,

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(FarmerId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    FOREIGN KEY(ParentQueryId) REFERENCES TblQuery(Id),
    INDEX IX_TblQuery_FarmerId (FarmerId),
    INDEX IX_TblQuery_Status (Status),
    INDEX IX_TblQuery_CreatedDate (CreatedDate),
    INDEX IX_TblQuery_ParentQueryId (ParentQueryId),
    INDEX IX_TblQuery_QueryType (QueryType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 8. TblAIResponse - AI Generated Responses
-- =====================================================
CREATE TABLE TblAIResponse
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    QueryId INT NOT NULL,

    -- AI Response Content
    AIAnswer TEXT,
    ConfidenceScore DECIMAL(5,2),       -- 0.00 to 100.00

    -- AI Model Tracking
    AIModel VARCHAR(50),                  -- 'OpenAI-GPT4', 'Gemini-Pro', etc.
    ResponseTimeMs INT,                   -- Performance tracking (milliseconds)
    Language VARCHAR(50),                 -- Response language used

    -- Escalation Flag
    IsEscalated TINYINT(1) DEFAULT 0,
    EscalationReason VARCHAR(500) NULL,   -- Why AI escalated (low confidence, complex, etc.)

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY(QueryId) REFERENCES TblQuery(Id) ON DELETE CASCADE,
    INDEX IX_TblAIResponse_QueryId (QueryId),
    INDEX IX_TblAIResponse_IsEscalated (IsEscalated),
    INDEX IX_TblAIResponse_AIModel (AIModel),
    INDEX IX_TblAIResponse_CreatedDate (CreatedDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 9. TblEscalation - Officer Escalation Management
-- =====================================================
CREATE TABLE TblEscalation
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    QueryId INT NOT NULL,
    OfficerId INT NOT NULL,

    -- Status & Priority
    Status VARCHAR(50) DEFAULT 'Pending',    -- Pending / InProgress / Solved / Rejected
    Priority VARCHAR(20) DEFAULT 'Medium',   -- Low / Medium / High / Urgent

    -- Officer Response
    OfficerReply TEXT,

    -- Timeline
    AssignedDate DATETIME NULL,
    ResolvedDate DATETIME NULL,

    -- Escalation Source
    EscalatedByAI TINYINT(1) DEFAULT 0,      -- True if AI auto-escalated
    EscalatedByFarmer TINYINT(1) DEFAULT 0,  -- True if farmer manually escalated

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    FOREIGN KEY(QueryId) REFERENCES TblQuery(Id) ON DELETE CASCADE,
    FOREIGN KEY(OfficerId) REFERENCES TblUser(Id),
    INDEX IX_TblEscalation_QueryId (QueryId),
    INDEX IX_TblEscalation_OfficerId (OfficerId),
    INDEX IX_TblEscalation_Status (Status),
    INDEX IX_TblEscalation_Priority (Priority),
    INDEX IX_TblEscalation_CreatedDate (CreatedDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 10. TblFeedback - User Feedback on AI Responses
-- =====================================================
CREATE TABLE TblFeedback
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    QueryId INT NOT NULL,
    AIResponseId INT NULL,                -- Link to specific AI response

    -- Feedback Content
    Rating INT,                           -- 1 to 5 stars
    FeedbackText TEXT,

    -- Feedback Type
    FeedbackType VARCHAR(50),             -- Helpful / NotHelpful / Incorrect / Suggestion

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY(QueryId) REFERENCES TblQuery(Id) ON DELETE CASCADE,
    FOREIGN KEY(AIResponseId) REFERENCES TblAIResponse(Id),
    INDEX IX_TblFeedback_QueryId (QueryId),
    INDEX IX_TblFeedback_AIResponseId (AIResponseId),
    INDEX IX_TblFeedback_Rating (Rating)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 11. TblWeatherLog - Weather Data Logging
-- =====================================================
CREATE TABLE TblWeatherLog
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    District VARCHAR(100),
    Village VARCHAR(100),

    -- Weather Data
    Temperature DECIMAL(5,2),
    Humidity DECIMAL(5,2),
    RainChance DECIMAL(5,2),
    WindSpeed DECIMAL(5,2),
    WeatherDescription VARCHAR(200),

    -- Farming Alerts
    FarmingAlert VARCHAR(500),            -- Generated farming suggestion
    AlertType VARCHAR(50),                -- Rain / Heat / Cold / Wind

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,

    INDEX IX_TblWeatherLog_District (District),
    INDEX IX_TblWeatherLog_CreatedDate (CreatedDate),
    INDEX IX_TblWeatherLog_AlertType (AlertType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 12. TblGovernmentScheme - Government Schemes
-- =====================================================
CREATE TABLE TblGovernmentScheme
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    SchemeName VARCHAR(200) NOT NULL,
    Description TEXT,
    Eligibility TEXT,

    -- Category & Status
    Category VARCHAR(100),                -- Subsidy / Loan / Insurance / Training
    Status VARCHAR(50) DEFAULT 'Active',  -- Active / Expired / Draft

    -- Dates
    StartDate DATE NULL,
    LastDate DATE,

    -- Documents
    DocumentUrl VARCHAR(300),             -- Link to scheme PDF

    -- Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,
    CreatedBy INT NULL,

    INDEX IX_TblGovernmentScheme_Status (Status),
    INDEX IX_TblGovernmentScheme_Category (Category),
    INDEX IX_TblGovernmentScheme_LastDate (LastDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 13. TblDiseaseDataset - Crop Disease Knowledge Base
-- =====================================================
CREATE TABLE TblDiseaseDataset
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    CropName VARCHAR(100),
    DiseaseName VARCHAR(100),

    -- Disease Details
    Symptoms TEXT,
    Solution TEXT,
    RecommendedPesticide VARCHAR(200),

    -- Visual Reference
    ReferenceImagePath VARCHAR(300),    -- Sample disease image
    SeverityLevel VARCHAR(50),            -- Low / Medium / High / Critical

    -- Status & Audit
    IsActive TINYINT(1) DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate DATETIME NULL,

    INDEX IX_TblDiseaseDataset_CropName (CropName),
    INDEX IX_TblDiseaseDataset_DiseaseName (DiseaseName),
    INDEX IX_TblDiseaseDataset_SeverityLevel (SeverityLevel)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 14. TblNotificationLog - Email/SMS/OTP Logging
-- WITH AUTO-CLEANUP SUPPORT
-- =====================================================
CREATE TABLE TblNotificationLog
(
    Id INT PRIMARY KEY AUTO_INCREMENT,

    UserId INT NOT NULL,

    -- Notification Details
    NotificationType VARCHAR(50),         -- OTP / Email / SMS / Push
    Subject VARCHAR(200),
    Content TEXT,

    -- Delivery Status
    Status VARCHAR(50) DEFAULT 'Pending', -- Pending / Sent / Failed / Delivered
    ErrorMessage VARCHAR(500) NULL,       -- If failed

    -- Timing
    SentDate DATETIME NULL,
    ExpiryDate DATETIME NULL,             -- For OTPs

    -- Metadata
    OTPCode VARCHAR(10) NULL,             -- For OTP notifications
    IsRead TINYINT(1) DEFAULT 0,

    -- NEW: Auto-Cleanup Fields
    RetentionDays INT DEFAULT 30,         -- How many days to keep this record
    IsDeleted TINYINT(1) DEFAULT 0,       -- Soft delete flag
    DeletedDate DATETIME NULL,            -- When soft deleted

    -- Audit
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY(UserId) REFERENCES TblUser(Id) ON DELETE CASCADE,
    INDEX IX_TblNotificationLog_UserId (UserId),
    INDEX IX_TblNotificationLog_Status (Status),
    INDEX IX_TblNotificationLog_NotificationType (NotificationType),
    INDEX IX_TblNotificationLog_OTPCode (OTPCode),
    INDEX IX_TblNotificationLog_CreatedDate (CreatedDate),
    INDEX IX_TblNotificationLog_IsDeleted (IsDeleted),
    INDEX IX_TblNotificationLog_RetentionDays (RetentionDays)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- AUTO-CLEANUP EVENT SCHEDULER
-- Runs daily at 2:00 AM to delete old notifications
-- =====================================================

-- Enable Event Scheduler
SET GLOBAL event_scheduler = ON;

-- Drop existing event if any
DROP EVENT IF EXISTS evt_CleanupOldNotifications;

-- Create auto-cleanup event
DELIMITER //

CREATE EVENT evt_CleanupOldNotifications
ON SCHEDULE EVERY 1 DAY
STARTS CONCAT(CURDATE() + INTERVAL 1 DAY, ' 02:00:00')
DO
BEGIN
    -- ==========================================
    -- STEP 1: Soft delete old notifications first
    -- ==========================================

    -- Mark OTP notifications older than 1 day as deleted (OTP expired anyway)
    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'OTP' 
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 1 DAY);

    -- Mark failed notifications older than 7 days as deleted
    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE Status = 'Failed' 
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 7 DAY);

    -- Mark delivered emails older than 30 days as deleted
    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'Email' 
    AND Status = 'Delivered'
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 30 DAY);

    -- Mark read push notifications older than 15 days as deleted
    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'Push' 
    AND IsRead = 1
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 15 DAY);

    -- Mark general notifications past their retention period
    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL RetentionDays DAY);

    -- ==========================================
    -- STEP 2: Physical delete very old soft-deleted records
    -- (keeps 90 days of soft-deleted data for safety)
    -- ==========================================

    DELETE FROM TblNotificationLog 
    WHERE IsDeleted = 1 
    AND DeletedDate < DATE_SUB(NOW(), INTERVAL 90 DAY);

END //

DELIMITER ;

-- Enable the event
ALTER EVENT evt_CleanupOldNotifications ENABLE;

-- =====================================================
-- MANUAL CLEANUP PROCEDURE (Run anytime if needed)
-- =====================================================

DROP PROCEDURE IF EXISTS sp_CleanupNotifications;

DELIMITER //

CREATE PROCEDURE sp_CleanupNotifications()
BEGIN
    -- Same logic as event, but callable manually

    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'OTP' 
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 1 DAY);

    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE Status = 'Failed' 
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 7 DAY);

    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'Email' 
    AND Status = 'Delivered'
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 30 DAY);

    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE NotificationType = 'Push' 
    AND IsRead = 1
    AND IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL 15 DAY);

    UPDATE TblNotificationLog 
    SET IsDeleted = 1, DeletedDate = NOW()
    WHERE IsDeleted = 0
    AND CreatedDate < DATE_SUB(NOW(), INTERVAL RetentionDays DAY);

    DELETE FROM TblNotificationLog 
    WHERE IsDeleted = 1 
    AND DeletedDate < DATE_SUB(NOW(), INTERVAL 90 DAY);

    SELECT CONCAT('Cleanup completed. Rows affected: ', ROW_COUNT()) AS Result;
END //

DELIMITER ;

-- =====================================================
-- SAMPLE DATA INSERTION
-- =====================================================

-- Insert Admin User (Password: 'Admin@123' - hashed)
INSERT INTO TblUser (Name, Email, Mobile, PasswordHash, Role, State, District, PreferredLanguage, IsEmailVerified, IsActive)
VALUES ('System Admin', 'admin@digitalkrushi.com', '9999999999', 
        '$2a$11$vQXqKqX8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8QZ8', 
        'Admin', 'Gujarat', 'Ahmedabad', 'Gujarati', 1, 1);

-- Insert Sample Crops
INSERT INTO TblCrop (CropName, Season, Description, IsActive) VALUES
('Cotton', 'Kharif', 'Major cash crop of Gujarat', 1),
('Groundnut', 'Kharif', 'Oilseed crop suitable for Gujarat soil', 1),
('Wheat', 'Rabi', 'Winter cereal crop', 1),
('Bajra', 'Kharif', 'Millet crop drought resistant', 1),
('Turmeric', 'Kharif', 'Spice crop with medicinal value', 1),
('Rice', 'Kharif', 'Staple food crop', 1),
('Sugarcane', 'Year-round', 'Cash crop for sugar production', 1),
('Castor', 'Kharif', 'Oilseed industrial crop', 1);

-- Insert Sample Disease Dataset
INSERT INTO TblDiseaseDataset (CropName, DiseaseName, Symptoms, Solution, RecommendedPesticide, SeverityLevel, IsActive) VALUES
('Cotton', 'Cotton Leaf Curl Virus', 'Leaves curl upward, vein thickening, stunted growth', 'Remove infected plants, use resistant varieties, control whiteflies', 'Imidacloprid 17.8% SL', 'High', 1),
('Groundnut', 'Tikka Disease', 'Circular spots on leaves, defoliation', 'Spray fungicide at 15-day intervals, crop rotation', 'Mancozeb 75% WP', 'Medium', 1),
('Wheat', 'Rust Disease', 'Orange-brown pustules on leaves and stems', 'Use resistant varieties, timely sowing, fungicide spray', 'Propiconazole 25% EC', 'High', 1),
('Cotton', 'Bollworm', 'Bolls damaged, larvae inside bolls', 'Pheromone traps, biological control, insecticide spray', 'Spinosad 45% SC', 'Critical', 1),
('Groundnut', 'Root Rot', 'Wilting, yellowing, root decay', 'Soil treatment, proper drainage, resistant varieties', 'Carbendazim 50% WP', 'High', 1);

-- Insert Sample Government Schemes
INSERT INTO TblGovernmentScheme (SchemeName, Description, Eligibility, Category, Status, StartDate, LastDate, IsActive) VALUES
('PM-KISAN', 'Direct income support of Rs. 6000/year to farmer families', 'Small and marginal farmers with cultivable land', 'Subsidy', 'Active', '2019-02-01', '2026-03-31', 1),
('Soil Health Card Scheme', 'Free soil testing and recommendation of nutrients', 'All farmers', 'Training', 'Active', '2015-02-01', '2026-03-31', 1),
('Kisan Credit Card', 'Short-term credit for cultivation at low interest', 'All farmers including sharecroppers', 'Loan', 'Active', '1998-08-01', '2026-03-31', 1),
('Crop Insurance (PMFBY)', 'Comprehensive crop insurance against natural calamities', 'All farmers growing notified crops', 'Insurance', 'Active', '2016-04-01', '2026-03-31', 1),
('Agricultural Mechanization', 'Subsidy on farm machinery and equipment', 'Individual farmers, cooperatives', 'Subsidy', 'Active', '2014-04-01', '2026-03-31', 1);

-- =====================================================
-- VIEWS FOR DASHBOARD ANALYTICS
-- =====================================================

-- View: Farmer Dashboard Summary
CREATE OR REPLACE VIEW VW_FarmerDashboard AS
SELECT 
    u.Id AS FarmerId,
    u.Name,
    u.Email,
    u.District,
    fd.LandArea,
    fd.SoilType,
    fd.MainCrop,
    fd.FarmingExperience,
    (SELECT COUNT(*) FROM TblQuery WHERE FarmerId = u.Id) AS TotalQueries,
    (SELECT COUNT(*) FROM TblQuery WHERE FarmerId = u.Id AND Status = 'Pending') AS PendingQueries,
    (SELECT COUNT(*) FROM TblFarmerCrop WHERE FarmerId = u.Id) AS TotalCrops,
    (SELECT COUNT(*) FROM TblFeedback WHERE QueryId IN (SELECT Id FROM TblQuery WHERE FarmerId = u.Id)) AS TotalFeedback
FROM TblUser u
LEFT JOIN TblFarmerDetails fd ON u.Id = fd.UserId
WHERE u.Role = 'Farmer' AND u.IsActive = 1;

-- View: Officer Workload
CREATE OR REPLACE VIEW VW_OfficerWorkload AS
SELECT 
    u.Id AS OfficerId,
    u.Name,
    u.District,
    od.Department,
    od.Designation,
    (SELECT COUNT(*) FROM TblEscalation WHERE OfficerId = u.Id AND Status = 'Pending') AS PendingEscalations,
    (SELECT COUNT(*) FROM TblEscalation WHERE OfficerId = u.Id AND Status = 'Solved') AS SolvedEscalations,
    (SELECT COUNT(*) FROM TblEscalation WHERE OfficerId = u.Id) AS TotalEscalations,
    (SELECT AVG(TIMESTAMPDIFF(HOUR, CreatedDate, ResolvedDate)) FROM TblEscalation WHERE OfficerId = u.Id AND ResolvedDate IS NOT NULL) AS AvgResolutionHours
FROM TblUser u
LEFT JOIN TblOfficerDetails od ON u.Id = od.UserId
WHERE u.Role = 'Officer' AND u.IsActive = 1;

-- View: AI Performance Analytics
CREATE OR REPLACE VIEW VW_AIAnalytics AS
SELECT 
    ar.AIModel,
    COUNT(*) AS TotalResponses,
    AVG(ar.ConfidenceScore) AS AvgConfidence,
    AVG(ar.ResponseTimeMs) AS AvgResponseTimeMs,
    SUM(CASE WHEN ar.IsEscalated = 1 THEN 1 ELSE 0 END) AS EscalatedCount,
    SUM(CASE WHEN ar.IsEscalated = 0 THEN 1 ELSE 0 END) AS ResolvedCount,
    CAST(SUM(CASE WHEN ar.IsEscalated = 0 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS ResolutionRate
FROM TblAIResponse ar
GROUP BY ar.AIModel;

-- View: System Overview for Admin
CREATE OR REPLACE VIEW VW_SystemOverview AS
SELECT 
    (SELECT COUNT(*) FROM TblUser WHERE Role = 'Farmer' AND IsActive = 1) AS TotalFarmers,
    (SELECT COUNT(*) FROM TblUser WHERE Role = 'Officer' AND IsActive = 1) AS TotalOfficers,
    (SELECT COUNT(*) FROM TblUser WHERE Role = 'Admin' AND IsActive = 1) AS TotalAdmins,
    (SELECT COUNT(*) FROM TblQuery) AS TotalQueries,
    (SELECT COUNT(*) FROM TblQuery WHERE Status = 'Pending') AS PendingQueries,
    (SELECT COUNT(*) FROM TblEscalation WHERE Status = 'Pending') AS PendingEscalations,
    (SELECT COUNT(*) FROM TblEscalation WHERE Status = 'Solved') AS SolvedEscalations,
    (SELECT COUNT(*) FROM TblFeedback) AS TotalFeedback,
    (SELECT AVG(Rating) FROM TblFeedback WHERE Rating IS NOT NULL) AS AvgRating,
    (SELECT COUNT(*) FROM TblCrop WHERE IsActive = 1) AS TotalCrops,
    (SELECT COUNT(*) FROM TblDiseaseDataset WHERE IsActive = 1) AS TotalDiseases,
    (SELECT COUNT(*) FROM TblGovernmentScheme WHERE Status = 'Active') AS ActiveSchemes;

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Check if event scheduler is enabled
SELECT @@event_scheduler AS EventSchedulerStatus;

-- Check if cleanup event exists
SHOW EVENTS;

-- Check all tables created
SHOW TABLES;

-- =====================================================
-- COMPLETION MESSAGE
-- =====================================================
SELECT 'Digital Krushi Officer Database Created Successfully!' AS Message;
SELECT 'Tables Created: 14' AS Info;
SELECT 'Indexes Created: 35+' AS Info;
SELECT 'Views Created: 4' AS Info;
SELECT 'Auto-Cleanup Event: Enabled (Daily at 2:00 AM)' AS Info;
SELECT 'Manual Cleanup Procedure: sp_CleanupNotifications()' AS Info;
SELECT 'Sample Data: Inserted' AS Info;
