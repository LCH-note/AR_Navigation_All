-- AlterTable
ALTER TABLE `facilities` ADD COLUMN `centerLat` DOUBLE NULL,
    ADD COLUMN `centerLng` DOUBLE NULL,
    ADD COLUMN `type` ENUM('indoor', 'outdoor', 'hybrid') NOT NULL DEFAULT 'indoor';
