-- TestData.sql
-- Run: sqlcmd -S "(localdb)\MSSQLLocalDB" -d "BookDb" -i TestData.sql

SET NOCOUNT ON;

-- ============================================
-- STEP 1: CLEAN EXISTING DATA (preserve admin)
-- ============================================
DELETE FROM RentalCopies;
DELETE FROM Rentals;
DELETE FROM Subscriptions;
DELETE FROM Subscribers;
DELETE FROM BookCopies;
DELETE FROM Books;
DELETE FROM Authors;
DELETE FROM Categories;
DELETE FROM Areas;
DELETE FROM Governorates;

-- ============================================
-- STEP 2: GOVERNORATES (4 Egyptian governorates)
-- ============================================
DECLARE @Cairo UNIQUEIDENTIFIER = NEWID();
DECLARE @Alexandria UNIQUEIDENTIFIER = NEWID();
DECLARE @Giza UNIQUEIDENTIFIER = NEWID();
DECLARE @Qalyubia UNIQUEIDENTIFIER = NEWID();

INSERT INTO Governorates (Id, Name) VALUES 
(@Cairo, N'القاهرة'),
(@Alexandria, N'الإسكندرية'),
(@Giza, N'الجيزة'),
(@Qalyubia, N'القليوبية');

-- ============================================
-- STEP 3: AREAS (2 per governorate = 8 total)
-- ============================================
DECLARE @NasrCity UNIQUEIDENTIFIER = NEWID();
DECLARE @Maadi UNIQUEIDENTIFIER = NEWID();
DECLARE @Montaza UNIQUEIDENTIFIER = NEWID();
DECLARE @SidiGabir UNIQUEIDENTIFIER = NEWID();
DECLARE @Dokki UNIQUEIDENTIFIER = NEWID();
DECLARE @Haram UNIQUEIDENTIFIER = NEWID();
DECLARE @Shubra UNIQUEIDENTIFIER = NEWID();
DECLARE @Qalyub UNIQUEIDENTIFIER = NEWID();

INSERT INTO Areas (Id, Name, GovernorateId) VALUES 
(@NasrCity, N'نصر سيتي', @Cairo),
(@Maadi, N'المعادي', @Cairo),
(@Montaza, N'المنتزه', @Alexandria),
(@SidiGabir, N'سيدي جابر', @Alexandria),
(@Dokki, N'الدقي', @Giza),
(@Haram, N'الهرم', @Giza),
(@Shubra, N'شبرا الخيمة', @Qalyubia),
(@Qalyub, N'القليوب', @Qalyubia);

-- ============================================
-- STEP 4: CATEGORIES (6 categories)
-- ============================================
DECLARE @CatFiction UNIQUEIDENTIFIER = NEWID();
DECLARE @CatScience UNIQUEIDENTIFIER = NEWID();
DECLARE @CatHistory UNIQUEIDENTIFIER = NEWID();
DECLARE @CatChildren UNIQUEIDENTIFIER = NEWID();
DECLARE @CatTech UNIQUEIDENTIFIER = NEWID();
DECLARE @CatBiography UNIQUEIDENTIFIER = NEWID();

INSERT INTO Categories (Id, Name, Description, Currentstste) VALUES 
(@CatFiction, N'الروايات', N'روايات وقصص خيالية', 1),
(@CatScience, N'العلوم', N'كتب علمية وتقنية', 1),
(@CatHistory, N'التاريخ', N'كتب تاريخية وسير', 1),
(@CatChildren, N'أطفال', N'كتب للأطفال والناشئة', 1),
(@CatTech, N'التكنولوجيا', N'برمجة وتقنية معلومات', 1),
(@CatBiography, N'السير الذاتية', N'سير شخصيات تاريخية ومعاصرة', 1);

-- ============================================
-- STEP 5: AUTHORS (6 authors)
-- ============================================
DECLARE @Author1 UNIQUEIDENTIFIER = NEWID(); -- نجيب محفوظ
DECLARE @Author2 UNIQUEIDENTIFIER = NEWID(); -- أحمد خالد توفيق
DECLARE @Author3 UNIQUEIDENTIFIER = NEWID(); -- ستيفن هوكينج
DECLARE @Author4 UNIQUEIDENTIFIER = NEWID(); -- جيهان السادات
DECLARE @Author5 UNIQUEIDENTIFIER = NEWID(); -- روبرت مارتن (Uncle Bob)
DECLARE @Author6 UNIQUEIDENTIFIER = NEWID(); -- ميشيل أوباما

INSERT INTO Authors (Id, Name, CreatedById, CreatedOn, IsDeleted, LastUpdatedById, LastUpdatedOn) VALUES 
(@Author1, N'نجيب محفوظ', 'seed', GETUTCDATE(), 0, NULL, NULL),
(@Author2, N'أحمد خالد توفيق', 'seed', GETUTCDATE(), 0, NULL, NULL),
(@Author3, N'Stephen Hawking', 'seed', GETUTCDATE(), 0, NULL, NULL),
(@Author4, N'جيهان السادات', 'seed', GETUTCDATE(), 0, NULL, NULL),
(@Author5, N'Robert C. Martin', 'seed', GETUTCDATE(), 0, NULL, NULL),
(@Author6, N'Michelle Obama', 'seed', GETUTCDATE(), 0, NULL, NULL);

-- ============================================
-- STEP 6: BOOKS (12 books, 2 per category)
-- ============================================
-- Fiction (2)
DECLARE @Book1 UNIQUEIDENTIFIER = NEWID(); -- بين القصرين
DECLARE @Book2 UNIQUEIDENTIFIER = NEWID(); -- مذكرات طالب

-- Science (2)
DECLARE @Book3 UNIQUEIDENTIFIER = NEWID(); -- تاريخ مختصر للزمن
DECLARE @Book4 UNIQUEIDENTIFIER = NEWID(); -- الكون في قشرة جوز

-- History (2)
DECLARE @Book5 UNIQUEIDENTIFIER = NEWID(); -- مصر القديمة
DECLARE @Book6 UNIQUEIDENTIFIER = NEWID(); -- الحرب العالمية الثانية

-- Children (2)
DECLARE @Book7 UNIQUEIDENTIFIER = NEWID(); -- الأمير الصغير
DECLARE @Book8 UNIQUEIDENTIFIER = NEWID(); -- هاري بوتر

-- Tech (2)
DECLARE @Book9 UNIQUEIDENTIFIER = NEWID(); -- Clean Code
DECLARE @Book10 UNIQUEIDENTIFIER = NEWID(); -- Design Patterns

-- Biography (2)
DECLARE @Book11 UNIQUEIDENTIFIER = NEWID(); -- كن نفسك
DECLARE @Book12 UNIQUEIDENTIFIER = NEWID(); -- Becoming

INSERT INTO books (Id, Name, ImageUrl, Description, IsAvailableForRental, CategoryId, SubCategoryId, CurrentState, AuthorId, Hall, ImagePublicId, ImageThumbnailUrl, Publisher, PublishingDate) VALUES
(@Book1, N'بين القصرين', NULL, N'أول ثلاثية نجيب محفوظ', 1, @CatFiction, NULL, 1, @Author1, NULL, NULL, NULL, N'دار الشروق', '1956-01-01'),
(@Book2, N'مذكرات طالب', NULL, N'رواية أحمد خالد توفيق الشهيرة', 1, @CatFiction, NULL, 1, @Author2, NULL, NULL, NULL, N'مؤسسة العربية الحديثة', '2001-01-01'),

(@Book3, N'تاريخ مختصر للزمن', NULL, N'كتاب ستيفن هوكينج الشهير', 1, @CatScience, NULL, 1, @Author3, NULL, NULL, NULL, N'بانتام بوكس', '1988-01-01'),
(@Book4, N'الكون في قشرة جوز', NULL, N'شرح مبسط للكون', 1, @CatScience, NULL, 1, @Author3, NULL, NULL, NULL, N'بانتام بوكس', '2001-01-01'),

(@Book5, N'مصر القديمة', NULL, N'تاريخ الحضارة المصرية', 1, @CatHistory, NULL, 1, @Author1, NULL, NULL, NULL, N'الهيئة المصرية العامة للكتاب', '1990-01-01'),
(@Book6, N'الحرب العالمية الثانية', NULL, N'تاريخ شامل للحرب', 1, @CatHistory, NULL, 1, @Author2, NULL, NULL, NULL, N'دار العلم', '2005-01-01'),

(@Book7, N'الأمير الصغير', NULL, N'قصة أنطوان دو سانت إكزوبيري', 1, @CatChildren, NULL, 1, @Author4, NULL, NULL, NULL, N'دار الهلال', '1943-01-01'),
(@Book8, N'هاري بوتر وحجر الفيلسوف', NULL, N'أول أجزاء هاري بوتر', 1, @CatChildren, NULL, 1, @Author6, NULL, NULL, NULL, N'Bloomsbury', '1997-01-01'),

(@Book9, N'Clean Code', NULL, N'دليل كتابة كود نظيف', 1, @CatTech, NULL, 1, @Author5, NULL, NULL, NULL, N'Prentice Hall', '2008-01-01'),
(@Book10, N'Design Patterns', NULL, N'أنماط التصميم القابلة لإعادة الاستخدام', 1, @CatTech, NULL, 1, @Author5, NULL, NULL, NULL, N'Addison-Wesley', '1994-01-01'),

(@Book11, N'كن نفسك', NULL, N'سيرة ذاتية لجلالة الملكة', 1, @CatBiography, NULL, 1, @Author4, NULL, NULL, NULL, N'دار الشروق', '2010-01-01'),
(@Book12, N'Becoming', NULL, N'مذكرات ميشيل أوباما', 1, @CatBiography, NULL, 1, @Author6, NULL, NULL, NULL, N'Crown Publishing', '2018-01-01');

-- ============================================
-- STEP 7: BOOK COPIES (24 copies, 2 per book)
-- ============================================
DECLARE @Copy1_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy1_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy2_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy2_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy3_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy3_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy4_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy4_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy5_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy5_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy6_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy6_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy7_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy7_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy8_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy8_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy9_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy9_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy10_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy10_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy11_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy11_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy12_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Copy12_2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO BookCopies (Id, BookId, IsAvailableForRental, EditionNumber, SerialNumber) VALUES
-- Fiction books
(@Copy1_1, @Book1, 1, 1, 1001),
(@Copy1_2, @Book1, 0, 1, 1002),
(@Copy2_1, @Book2, 1, 1, 2001),
(@Copy2_2, @Book2, 1, 1, 2002),

-- Science books
(@Copy3_1, @Book3, 1, 1, 3001),
(@Copy3_2, @Book3, 0, 1, 3002),
(@Copy4_1, @Book4, 1, 1, 4001),
(@Copy4_2, @Book4, 1, 1, 4002),

-- History books
(@Copy5_1, @Book5, 1, 1, 5001),
(@Copy5_2, @Book5, 1, 1, 5002),
(@Copy6_1, @Book6, 0, 1, 6001),
(@Copy6_2, @Book6, 1, 1, 6002),

-- Children books
(@Copy7_1, @Book7, 1, 1, 7001),
(@Copy7_2, @Book7, 1, 1, 7002),
(@Copy8_1, @Book8, 0, 1, 8001),
(@Copy8_2, @Book8, 1, 1, 8002),

-- Tech books
(@Copy9_1, @Book9, 1, 1, 9001),
(@Copy9_2, @Book9, 1, 1, 9002),
(@Copy10_1, @Book10, 0, 1, 10001),
(@Copy10_2, @Book10, 1, 1, 10002),

-- Biography books
(@Copy11_1, @Book11, 1, 1, 11001),
(@Copy11_2, @Book11, 1, 1, 11002),
(@Copy12_1, @Book12, 0, 1, 12001),
(@Copy12_2, @Book12, 1, 1, 12002);

-- ============================================
-- STEP 8: SUBSCRIBERS (8 subscribers, 1 blacklisted)
-- ============================================
DECLARE @Sub1 UNIQUEIDENTIFIER = NEWID(); -- أحمد محمد (Normal)
DECLARE @Sub2 UNIQUEIDENTIFIER = NEWID(); -- فاطمة علي (Normal)
DECLARE @Sub3 UNIQUEIDENTIFIER = NEWID(); -- محمد حسن (Normal)
DECLARE @Sub4 UNIQUEIDENTIFIER = NEWID(); -- سارة أحمد (Normal)
DECLARE @Sub5 UNIQUEIDENTIFIER = NEWID(); -- عمر خالد (Normal)
DECLARE @Sub6 UNIQUEIDENTIFIER = NEWID(); -- نورا سعيد (Normal)
DECLARE @Sub7 UNIQUEIDENTIFIER = NEWID(); -- كريم محمود (Blacklisted)
DECLARE @Sub8 UNIQUEIDENTIFIER = NEWID(); -- ليلى إبراهيم (Normal)

INSERT INTO Subscribers (Id, FirstName, LastName, DateOfBirth, NationalId, MobileNumber, HasWhatsApp, Email, ImageUrl, ImageThumbnailUrl, AreaId, GovernorateId, Address, IsBlackListed) VALUES
(@Sub1, N'أحمد', N'محمد', '1990-05-15', '12345678901234', '01012345678', 1, 'ahmed@test.com', NULL, NULL, @NasrCity, @Cairo, N'ش 10، عمارة 5، نصر سيتي', 0),
(@Sub2, N'فاطمة', N'علي', '1985-08-22', '23456789012345', '01023456789', 1, 'fatma@test.com', NULL, NULL, @Maadi, @Cairo, N'ميدان المعادي، عمارة 12', 0),
(@Sub3, N'محمد', N'حسن', '1992-03-10', '34567890123456', '01134567890', 0, 'mohamed@test.com', NULL, NULL, @Montaza, @Alexandria, N'ش جمال عبد الناصر، 45', 0),
(@Sub4, N'سارة', N'أحمد', '1988-11-30', '45678901234567', '01245678901', 1, 'sara@test.com', NULL, NULL, @SidiGabir, @Alexandria, N'كورنيش الإسكندرية، 78', 0),
(@Sub5, N'عمر', N'خالد', '1995-01-18', '56789012345678', '01556789012', 1, 'omar@test.com', NULL, NULL, @Dokki, @Giza, N'ش التحرير، 23، الدقي', 0),
(@Sub6, N'نورا', N'سعيد', '1991-07-25', '67890123456789', '01067890123', 0, 'nora@test.com', NULL, NULL, @Haram, @Giza, N'شارع الهرم، 156', 0),
(@Sub7, N'كريم', N'محمود', '1987-09-12', '78901234567890', '01178901234', 1, 'karim@test.com', NULL, NULL, @Shubra, @Qalyubia, N'ش 15 مايو، 89', 1), -- BLACKLISTED
(@Sub8, N'ليلى', N'إبراهيم', '1993-04-08', '89012345678901', '01289012345', 1, 'layla@test.com', NULL, NULL, @Qalyub, @Qalyubia, N'ميدان القليوب، 34', 0);

-- ============================================
-- STEP 9: SUBSCRIPTIONS (10 subs: 6 active, 2 expiring soon, 2 expired)
-- ============================================
DECLARE @Now DATETIME2 = GETUTCDATE();

INSERT INTO Subscriptions (Id, SubscriberId, StartDate, EndDate, CreatedById, CreatedOn) VALUES
-- Active subscriptions (6)
(NEWID(), @Sub1, DATEADD(month, -6, @Now), DATEADD(month, 6, @Now), 'seed', @Now),
(NEWID(), @Sub2, DATEADD(month, -3, @Now), DATEADD(month, 9, @Now), 'seed', @Now),
(NEWID(), @Sub3, DATEADD(month, -1, @Now), DATEADD(month, 11, @Now), 'seed', @Now),
(NEWID(), @Sub4, DATEADD(month, -4, @Now), DATEADD(month, 8, @Now), 'seed', @Now),
(NEWID(), @Sub5, DATEADD(month, -2, @Now), DATEADD(month, 10, @Now), 'seed', @Now),
(NEWID(), @Sub6, DATEADD(month, -5, @Now), DATEADD(month, 7, @Now), 'seed', @Now),

-- Expiring soon (2) - within 30 days
(NEWID(), @Sub7, DATEADD(month, -11, @Now), DATEADD(day, 3, @Now), 'seed', @Now),   -- Expires in 3 days
(NEWID(), @Sub8, DATEADD(month, -10, @Now), DATEADD(day, 10, @Now), 'seed', @Now),  -- Expires in 10 days

-- Expired (2)
(NEWID(), @Sub1, DATEADD(month, -18, @Now), DATEADD(month, -6, @Now), 'seed', @Now), -- Old expired
(NEWID(), @Sub2, DATEADD(month, -15, @Now), DATEADD(month, -3, @Now), 'seed', @Now);

-- ============================================
-- STEP 10: RENTALS (15 rentals: active, overdue, returned)
-- ============================================
DECLARE @Rental1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental3 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental4 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental5 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental6 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental7 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental8 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental9 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental10 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental11 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental12 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental13 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental14 UNIQUEIDENTIFIER = NEWID();
DECLARE @Rental15 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Rentals (Id, SubscriberId, StartDate, PenaltyPaid, TotalFine) VALUES
-- Active (5) - started recently, EndDate in future
(@Rental1, @Sub1, DATEADD(day, -10, @Now), 0, NULL),
(@Rental2, @Sub2, DATEADD(day, -5, @Now), 0, NULL),
(@Rental3, @Sub3, DATEADD(day, -15, @Now), 0, NULL),
(@Rental4, @Sub4, DATEADD(day, -3, @Now), 0, NULL),
(@Rental5, @Sub5, DATEADD(day, -7, @Now), 0, NULL),

-- Overdue (3) - EndDate was in past
(@Rental6, @Sub1, DATEADD(day, -30, @Now), 0, 15.00),
(@Rental7, @Sub2, DATEADD(day, -25, @Now), 0, 10.00),
(@Rental8, @Sub3, DATEADD(day, -40, @Now), 1, 20.00),

-- Returned (7) - various dates
(@Rental9, @Sub4, DATEADD(day, -60, @Now), 0, NULL),
(@Rental10, @Sub5, DATEADD(day, -45, @Now), 0, NULL),
(@Rental11, @Sub6, DATEADD(day, -90, @Now), 0, NULL),
(@Rental12, @Sub7, DATEADD(day, -120, @Now), 1, 25.00),
(@Rental13, @Sub8, DATEADD(day, -30, @Now), 0, NULL),
(@Rental14, @Sub1, DATEADD(day, -15, @Now), 0, NULL),
(@Rental15, @Sub2, DATEADD(day, -20, @Now), 0, NULL);

-- ============================================
-- STEP 11: RENTAL COPIES (18 entries)
-- ============================================
INSERT INTO RentalCopies (RentalId, BookCopyId, RentalDate, EndDate, ReturnDate, ExtendedOn) VALUES
-- Active rentals (5) - no ReturnDate, EndDate in future
(@Rental1, @Copy1_1, DATEADD(day, -10, @Now), DATEADD(day, 20, @Now), NULL, NULL),
(@Rental2, @Copy2_1, DATEADD(day, -5, @Now), DATEADD(day, 25, @Now), NULL, NULL),
(@Rental3, @Copy3_1, DATEADD(day, -15, @Now), DATEADD(day, 15, @Now), NULL, NULL),
(@Rental4, @Copy4_1, DATEADD(day, -3, @Now), DATEADD(day, 27, @Now), NULL, NULL),
(@Rental5, @Copy5_1, DATEADD(day, -7, @Now), DATEADD(day, 23, @Now), NULL, NULL),

-- Overdue rentals (3) - EndDate in past, no ReturnDate
(@Rental6, @Copy1_2, DATEADD(day, -30, @Now), DATEADD(day, -10, @Now), NULL, NULL),
(@Rental7, @Copy3_2, DATEADD(day, -25, @Now), DATEADD(day, -5, @Now), NULL, NULL),
(@Rental8, @Copy6_1, DATEADD(day, -40, @Now), DATEADD(day, -20, @Now), NULL, NULL),

-- Returned rentals (7) - have ReturnDate
(@Rental9, @Copy7_1, DATEADD(day, -60, @Now), DATEADD(day, -40, @Now), DATEADD(day, -42, @Now), NULL),
(@Rental10, @Copy8_1, DATEADD(day, -45, @Now), DATEADD(day, -25, @Now), DATEADD(day, -27, @Now), NULL),
(@Rental11, @Copy9_1, DATEADD(day, -90, @Now), DATEADD(day, -70, @Now), DATEADD(day, -72, @Now), NULL),
(@Rental12, @Copy10_1, DATEADD(day, -120, @Now), DATEADD(day, -100, @Now), DATEADD(day, -102, @Now), NULL),
(@Rental13, @Copy11_1, DATEADD(day, -30, @Now), DATEADD(day, -10, @Now), DATEADD(day, -12, @Now), NULL),
(@Rental14, @Copy12_1, DATEADD(day, -15, @Now), DATEADD(day, 5, @Now), DATEADD(day, 3, @Now), NULL),
(@Rental15, @Copy7_2, DATEADD(day, -20, @Now), DATEADD(day, 0, @Now), DATEADD(day, -2, @Now), NULL);

-- ============================================
-- STEP 12: VERIFICATION
-- ============================================
SELECT 'Governorates' as TableName, COUNT(*) as Count FROM Governorates
UNION ALL SELECT 'Areas', COUNT(*) FROM Areas
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Authors', COUNT(*) FROM Authors
UNION ALL SELECT 'Books', COUNT(*) FROM books
UNION ALL SELECT 'BookCopies', COUNT(*) FROM BookCopies
UNION ALL SELECT 'Subscribers', COUNT(*) FROM Subscribers
UNION ALL SELECT 'Subscriptions', COUNT(*) FROM Subscriptions
UNION ALL SELECT 'Rentals', COUNT(*) FROM Rentals
UNION ALL SELECT 'RentalCopies', COUNT(*) FROM RentalCopies;

-- Dashboard stat verification
SELECT 
    (SELECT COUNT(*) FROM books) as TotalBooks,
    (SELECT COUNT(*) FROM BookCopies WHERE IsAvailableForRental = 1) as AvailableCopies,
    (SELECT COUNT(*) FROM Subscribers) as TotalSubscribers,
    (SELECT COUNT(*) FROM Subscriptions WHERE StartDate <= GETUTCDATE() AND EndDate >= GETUTCDATE()) as ActiveSubscriptions,
    (SELECT COUNT(*) FROM Rentals r WHERE EXISTS (SELECT 1 FROM RentalCopies rc WHERE rc.RentalId = r.Id AND rc.ReturnDate IS NULL)) as ActiveRentals,
    (SELECT COUNT(*) FROM Rentals r WHERE EXISTS (SELECT 1 FROM RentalCopies rc WHERE rc.RentalId = r.Id AND rc.ReturnDate IS NULL AND rc.EndDate < GETUTCDATE())) as OverdueRentals,
    (SELECT COUNT(*) FROM Subscriptions WHERE EndDate >= GETUTCDATE() AND EndDate <= DATEADD(day, 30, GETUTCDATE())) as ExpiringSubscriptions,
    (SELECT COUNT(*) FROM Subscribers WHERE IsBlackListed = 1) as BlacklistedSubscribers;