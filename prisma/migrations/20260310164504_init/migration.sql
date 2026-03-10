-- AddForeignKey
ALTER TABLE `anchors` ADD CONSTRAINT `anchors_facilityId_fkey` FOREIGN KEY (`facilityId`) REFERENCES `facilities`(`id`) ON DELETE CASCADE ON UPDATE CASCADE;
