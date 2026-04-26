-- AlterTable
ALTER TABLE `places` ADD COLUMN `arMarkerId` VARCHAR(191) NULL,
    ADD COLUMN `feature` VARCHAR(191) NULL,
    ADD COLUMN `imagePath` VARCHAR(191) NULL;

-- CreateTable
CREATE TABLE `reviews` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `placeId` INTEGER NOT NULL,
    `star` INTEGER NOT NULL,
    `content` TEXT NULL,
    `nickname` VARCHAR(191) NOT NULL DEFAULT '익명',
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),

    INDEX `reviews_placeId_idx`(`placeId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateIndex
CREATE INDEX `places_arMarkerId_idx` ON `places`(`arMarkerId`);

-- AddForeignKey
ALTER TABLE `reviews` ADD CONSTRAINT `reviews_placeId_fkey` FOREIGN KEY (`placeId`) REFERENCES `places`(`id`) ON DELETE CASCADE ON UPDATE CASCADE;
