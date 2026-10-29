-- =====================================================================
--  АИС "Аукционы" -- добавление фотографий аукционов и лотов
--  Скрипт идемпотентный: колонки добавляются только если их нет.
--  Выполнять на существующей базе (не запускать schema.sql — он удаляет таблицы).
-- =====================================================================

SET NOCOUNT ON;

-- ---------------------------------------------------------------------
-- 1. Колонки с именем файла изображения
-- ---------------------------------------------------------------------
IF COL_LENGTH('dbo.UP03_Auktsiony', 'Foto') IS NULL
    ALTER TABLE dbo.UP03_Auktsiony ADD Foto VARCHAR(260) NULL;

IF COL_LENGTH('dbo.UP03_Loty', 'Foto') IS NULL
    ALTER TABLE dbo.UP03_Loty ADD Foto VARCHAR(260) NULL;

-- ---------------------------------------------------------------------
-- 2. Привязка демонстрационных фотографий
--    Файлы лежат в AuctionSystem\Images\Auctions и AuctionSystem\Images\Lots
-- ---------------------------------------------------------------------
UPDATE dbo.UP03_Auktsiony
SET Foto = 'auction_antique.jpg'
WHERE Foto IS NULL
  AND Nazvanie LIKE N'%антиквариат%';

UPDATE dbo.UP03_Loty
SET Foto = 'lot_01_vase.jpg'
WHERE Foto IS NULL
  AND Naimenovanie LIKE N'%Ваза%';

UPDATE dbo.UP03_Loty
SET Foto = 'lot_02_candlestick.jpg'
WHERE Foto IS NULL
  AND (Naimenovanie LIKE N'%Подсвечник%' OR NomerLota = 2);

UPDATE dbo.UP03_Loty
SET Foto = 'lot_03_landscape.jpg'
WHERE Foto IS NULL
  AND Naimenovanie LIKE N'%пейзаж%';

UPDATE dbo.UP03_Loty
SET Foto = 'lot_04_box.jpg'
WHERE Foto IS NULL
  AND Naimenovanie LIKE N'%Шкатулка%';

PRINT 'Фотографии аукционов и лотов подключены.';
