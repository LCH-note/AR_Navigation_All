/*
  Warnings:

  - You are about to drop the column `code` on the `anchors` table. All the data in the column will be lost.
  - You are about to drop the column `x` on the `anchors` table. All the data in the column will be lost.
  - You are about to drop the column `y` on the `anchors` table. All the data in the column will be lost.
  - You are about to drop the column `yaw` on the `anchors` table. All the data in the column will be lost.
  - You are about to drop the column `z` on the `anchors` table. All the data in the column will be lost.
  - A unique constraint covering the columns `[facilityId,cloudAnchorId]` on the table `anchors` will be added. If there are existing duplicate values, this will fail.
  - Added the required column `cloudAnchorId` to the `anchors` table without a default value. This is not possible if the table is not empty.
  - Added the required column `localX` to the `anchors` table without a default value. This is not possible if the table is not empty.
  - Added the required column `localY` to the `anchors` table without a default value. This is not possible if the table is not empty.
  - Added the required column `localZ` to the `anchors` table without a default value. This is not possible if the table is not empty.

*/
-- DropForeignKey
ALTER TABLE `anchors` DROP FOREIGN KEY `anchors_facilityId_fkey`;

-- DropIndex
DROP INDEX `anchors_facilityId_code_key` ON `anchors`;

-- AlterTable
ALTER TABLE `anchors` DROP COLUMN `code`,
    DROP COLUMN `x`,
    DROP COLUMN `y`,
    DROP COLUMN `yaw`,
    DROP COLUMN `z`,
    ADD COLUMN `cloudAnchorId` VARCHAR(191) NOT NULL,
    ADD COLUMN `localX` DOUBLE NOT NULL,
    ADD COLUMN `localY` DOUBLE NOT NULL,
    ADD COLUMN `localYawDeg` DOUBLE NULL,
    ADD COLUMN `localZ` DOUBLE NOT NULL,
    ADD COLUMN `note` VARCHAR(191) NULL;

-- AlterTable
ALTER TABLE `places` ADD COLUMN `category` VARCHAR(191) NULL;

-- CreateIndex
CREATE UNIQUE INDEX `anchors_facilityId_cloudAnchorId_key` ON `anchors`(`facilityId`, `cloudAnchorId`);

-- CreateIndex
CREATE INDEX `places_category_idx` ON `places`(`category`);
