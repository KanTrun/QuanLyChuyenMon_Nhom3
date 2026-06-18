USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @summary NVARCHAR(MAX) = N'{"ocrStatus":"OCR_EXTRACTED","note":"Nội dung chính và lưu đồ đã được nhập từ OCR; PDF scan nguồn được giữ kèm để kiểm soát."}';

UPDATE med.professional_procedures
SET description = N'Trích xuất từ PDF scan 2145; PDF nguồn được gắn kèm làm căn cứ kiểm soát nội dung.',
    updated_at = SYSUTCDATETIME()
WHERE procedure_code IN (N'QT.KSNK.09', N'QT.KSNK.12', N'QT.KSNK.16', N'QT.KSNK.17');

UPDATE v
SET summary = @summary,
    change_reason = N'Nhập nội dung chính từ OCR PDF scan 2145',
    updated_at = SYSUTCDATETIME()
FROM med.procedure_versions v
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
WHERE p.procedure_code IN (N'QT.KSNK.09', N'QT.KSNK.12', N'QT.KSNK.16', N'QT.KSNK.17');

UPDATE r
SET summary = N'Ban hành theo PDF scan số 2145; nội dung chính và lưu đồ đã nhập từ OCR, PDF nguồn được giữ kèm để kiểm soát.'
FROM med.procedure_revision_entries r
JOIN med.procedure_versions v ON v.procedure_version_id = r.procedure_version_id
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
WHERE p.procedure_code IN (N'QT.KSNK.09', N'QT.KSNK.12', N'QT.KSNK.16', N'QT.KSNK.17')
  AND r.display_order = 1;

DECLARE @sections TABLE (
    code NVARCHAR(64),
    section_order INT,
    content NVARCHAR(MAX)
);

INSERT INTO @sections VALUES
(N'QT.KSNK.09',1,N'Thống nhất quy trình xử lý dụng cụ phẫu thuật; tăng cường thực hành tốt xử lý dụng cụ, hạn chế thấp nhất nguy cơ nhiễm khuẩn, bảo đảm an toàn người bệnh và chất lượng phẫu thuật.'),
(N'QT.KSNK.09',2,N'Áp dụng cho khoa Gây mê hồi sức và khoa Kiểm soát nhiễm khuẩn trong tiếp nhận, xử lý, tiệt khuẩn, lưu trữ và bàn giao dụng cụ phẫu thuật phục vụ phẫu thuật tại Bệnh viện Ung Bướu.'),
(N'QT.KSNK.09',3,N'Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.
Thông tư 16/2018/TT-BYT ngày 20/7/2018 của Bộ Y tế quy định về kiểm soát nhiễm khuẩn trong các cơ sở khám bệnh, chữa bệnh.
Quyết định 3916/QĐ-BYT ngày 28/8/2017 của Bộ Y tế về Hướng dẫn xử lý dụng cụ phẫu thuật nội soi trong các cơ sở khám bệnh, chữa bệnh.'),
(N'QT.KSNK.09',4,N'Tiệt khuẩn: quá trình tiêu diệt hoặc loại bỏ tất cả dạng vi sinh vật sống, bao gồm bào tử vi khuẩn.
Khử khuẩn: quá trình loại bỏ hầu hết hoặc tất cả vi sinh vật gây bệnh trên dụng cụ nhưng không diệt bào tử vi khuẩn; gồm mức độ thấp, trung bình và cao.
Làm sạch: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ; là bước bắt buộc trước khử khuẩn/tiệt khuẩn.
Từ viết tắt: LS - làm sạch; KK - khử khuẩn; TK - tiệt khuẩn; KSNK - kiểm soát nhiễm khuẩn; NV - nhân viên; PHCN - phòng hộ cá nhân; ĐD - điều dưỡng; GMHS - gây mê hồi sức.'),
(N'QT.KSNK.09',8,N'5.2.1. Làm sạch dụng cụ tại khoa GMHS: nhân viên mang PHCN, pha hóa chất theo khuyến cáo, loại bỏ chất thải, tháo rời/mở khớp, ngâm enzyme đúng thời gian, chà rửa, tráng nước sạch và làm khô.

5.2.2. Giao nhận dụng cụ sau khi làm sạch: ĐD dụng cụ kiểm tra dụng cụ sạch, khô; vận chuyển trong thùng có nắp đến khoa KSNK; hai bên kiểm đếm, ký sổ và ghi nhận hư hỏng, thất lạc hoặc nhu cầu khẩn cấp.

5.2.3. Làm sạch, khử khuẩn tại khoa KSNK: NV KSNK kiểm tra độ sạch và số lượng, xử lý lại bằng máy rửa/khử khuẩn hoặc thao tác thủ công, tuân thủ hóa chất, thời gian, tráng và làm khô.

5.2.4. Bảo dưỡng - kiểm tra: bảo dưỡng theo hướng dẫn nhà sản xuất; kiểm tra khớp, khóa, lòng ống, răng cưa, bề mặt, độ sắc bén, độ khô, gỉ sét và hư hỏng.

5.2.5. Đóng gói: đóng gói bằng túi ép, hộp hoặc khay; đặt chỉ thị hóa học, hàn kín, ghi nhãn lô, ngày đóng gói, người đóng gói và hạn dùng.

5.2.6. Tiệt khuẩn và giám sát chất lượng: vận hành máy hấp phù hợp loại dụng cụ, theo dõi thông số chu trình, chỉ thị cơ học, hóa học, sinh học/PCD; cách ly và xử lý lại mẻ không đạt.

5.2.7. Lưu trữ: lưu dụng cụ đạt yêu cầu tại kho vô khuẩn, bảo đảm bao gói nguyên vẹn, khô sạch, đúng hạn dùng và sắp xếp nhập trước - xuất trước.

5.2.8. Giao nhận sau tiệt khuẩn: kiểm tra tình trạng vô khuẩn, nhãn, hạn dùng, số lượng; khoa GMHS tiếp nhận, ký sổ và trả lại dụng cụ rách/ướt/quá hạn để xử lý lại.'),
(N'QT.KSNK.09',10,N'BM.KSNK.09.01 - BM.KSNK.09.11 và Phụ lục I-X theo PDF nguồn: bảng kiểm chuẩn bị/làm sạch, giao nhận, khử khuẩn, đóng gói, giám sát chất lượng tiệt khuẩn, lưu trữ và cấp phát dụng cụ phẫu thuật.'),

(N'QT.KSNK.12',1,N'Thống nhất quy trình xử lý dụng cụ y tế nhằm cung cấp đầy đủ và duy trì chất lượng khử khuẩn, tiệt khuẩn cho dụng cụ y tế sử dụng lại trong bệnh viện, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng khám chữa bệnh.'),
(N'QT.KSNK.12',2,N'Áp dụng cho các khoa lâm sàng, cận lâm sàng đang quản lý dụng cụ y tế gửi khoa Kiểm soát nhiễm khuẩn để xử lý tập trung; áp dụng cho nhân viên các khoa liên quan trong tiếp nhận, xử lý và bàn giao dụng cụ y tế.'),
(N'QT.KSNK.12',3,N'Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.
Thông tư 16/2018/TT-BYT ngày 20/7/2018 của Bộ Y tế quy định về kiểm soát nhiễm khuẩn trong các cơ sở khám bệnh, chữa bệnh.'),
(N'QT.KSNK.12',4,N'Tiệt khuẩn: quá trình tiêu diệt hoặc loại bỏ tất cả dạng vi sinh vật sống, bao gồm bào tử vi khuẩn.
Khử khuẩn: quá trình loại bỏ hầu hết hoặc tất cả vi sinh vật gây bệnh trên dụng cụ nhưng không diệt bào tử vi khuẩn.
Làm sạch: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ; là bước bắt buộc trước khử khuẩn/tiệt khuẩn.
Từ viết tắt: LS, KK, TK, KSNK, NV, PHCN, ĐD, VT/TBYT, ĐDT, TH theo PDF nguồn.'),
(N'QT.KSNK.12',8,N'5.2.1. Làm sạch, khử khuẩn tại khoa sử dụng: nhân viên mang PHCN, pha hóa chất, loại bỏ chất thải, tháo rời, ngâm enzyme, chà rửa, tráng, làm khô và khử khuẩn mức độ trung bình với dụng cụ có ngóc ngách hoặc dính máu/mủ/dịch tiết.

5.2.2. Giao nhận sau làm sạch, khử khuẩn: khoa sử dụng kiểm tra, ghi số lượng/chủng loại, bàn giao trực tiếp cho KSNK; hai bên kiểm đếm, ký nhận và xử lý sai lệch.

5.2.3. Làm sạch, khử khuẩn tại KSNK: NV KSNK kiểm tra độ sạch, số lượng, chủng loại, xử lý bằng máy rửa khử khuẩn hoặc bằng tay tùy loại dụng cụ.

5.2.4. Bảo dưỡng - kiểm tra: bảo dưỡng, kiểm tra chức năng, khóa khớp, lòng ống, vết bẩn, gỉ sét, biến dạng và độ khô; tách dụng cụ không đạt.

5.2.5. Đóng gói: đóng gói bằng túi ép, hộp hoặc khay; đặt chỉ thị hóa học, hàn/niêm kín, ghi nhãn ngày đóng gói, người đóng gói, lô hấp và hạn dùng.

5.2.6. Tiệt khuẩn và giám sát: chọn máy hấp phù hợp, giám sát thông số mẻ, chỉ thị hóa học, sinh học/PCD; ghi nhận và cách ly mẻ không đạt.

5.2.7. Lưu trữ: bảo quản dụng cụ tiệt khuẩn tại kho vô khuẩn, kiểm tra bao gói, nhãn, hạn dùng và sắp xếp nhập trước - xuất trước.

5.2.8. Giao nhận sau tiệt khuẩn: kiểm tra dụng cụ vô khuẩn trước khi giao; khoa sử dụng ký nhận, bảo quản đến khi dùng, trả lại dụng cụ hư bao gói/quá hạn/nghi nhiễm bẩn.'),
(N'QT.KSNK.12',10,N'BM.KSNK.12.01 - BM.KSNK.12.11 và Phụ lục I-V theo PDF nguồn: bảng kiểm chuẩn bị, làm sạch/khử khuẩn, đóng gói, giám sát tiệt khuẩn, giao nhận và lưu trữ dụng cụ y tế.'),

(N'QT.KSNK.16',1,N'Thống nhất quy trình khử khuẩn mức độ cao nhằm cung cấp đầy đủ và duy trì chất lượng khử khuẩn cho dụng cụ y tế sử dụng lại trong bệnh viện, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng điều trị.'),
(N'QT.KSNK.16',2,N'Áp dụng đối với dụng cụ bán thiết yếu và dụng cụ hỗ trợ hô hấp không thể tiệt khuẩn; áp dụng cho nhân viên các khoa lâm sàng, cận lâm sàng và khoa Kiểm soát nhiễm khuẩn được giao nhiệm vụ xử lý dụng cụ.'),
(N'QT.KSNK.16',3,N'Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.
Thông tư 16/2018/TT-BYT ngày 20/7/2018 của Bộ Y tế quy định về kiểm soát nhiễm khuẩn trong các cơ sở khám bệnh, chữa bệnh.'),
(N'QT.KSNK.16',4,N'Dụng cụ bán thiết yếu: dụng cụ tiếp xúc với niêm mạc hoặc da bị tổn thương.
Dụng cụ hỗ trợ hô hấp: dụng cụ sử dụng để hỗ trợ quá trình hô hấp hoặc kỹ thuật chăm sóc, điều trị đường hô hấp.
Khử khuẩn mức độ cao: quá trình tiêu diệt toàn bộ vi sinh vật và một số bào tử vi khuẩn.
Từ viết tắt: LS - làm sạch; KKMĐC - khử khuẩn mức độ cao; KSNK - kiểm soát nhiễm khuẩn; NV - nhân viên; NSX - nhà sản xuất; PHCN - phòng hộ cá nhân.'),
(N'QT.KSNK.16',8,N'5.2.1. Làm sạch dụng cụ: dụng cụ bán thiết yếu/hỗ trợ hô hấp sau sử dụng được xử lý tại khu riêng; nhân viên mang PHCN, pha enzyme, loại bỏ chất thải, tháo rời, xả nước, ngâm, chà rửa, tráng và làm khô.

5.2.2. Giao nhận sau làm sạch: khoa sử dụng kiểm tra sạch/khô, ghi số lượng/chủng loại và bàn giao cho KSNK; KSNK kiểm đếm, đối chiếu, ký nhận và yêu cầu xử lý lại nếu còn bẩn.

5.2.3. Khử khuẩn mức độ cao: chuẩn bị bồn/khay ngâm, que thử nồng độ, đồng hồ, khăn vô khuẩn, máy sấy và hóa chất còn hạn; kiểm tra nồng độ, ngâm ngập đúng thời gian, tráng, làm khô và chuyển đóng gói vô khuẩn.

5.2.4. Đóng gói, lưu trữ và giao nhận: đóng gói bằng túi ép đã tiệt khuẩn, ghi ngày đóng gói, nhân viên đóng gói, hạn sử dụng 14 ngày; lưu kho vô khuẩn và giao nhận theo bảng kiểm.'),
(N'QT.KSNK.16',10,N'BM.KSNK.16.01 - BM.KSNK.16.05 và Phụ lục I-III theo PDF nguồn: bảng kiểm làm sạch, giao nhận, khử khuẩn mức độ cao, đóng gói và giao nhận dụng cụ vô khuẩn.'),

(N'QT.KSNK.17',1,N'Tiệt khuẩn tay khoan nha khoa nhằm kiểm soát, phòng chống lây nhiễm chéo cho người bệnh trong thực hiện thủ thuật, đáp ứng yêu cầu an toàn người bệnh và nâng cao chất lượng điều trị.'),
(N'QT.KSNK.17',2,N'Áp dụng cho bác sĩ, trợ thủ nha khoa, nhân viên phòng khám răng miệng và nhân viên khoa Kiểm soát nhiễm khuẩn trong quá trình xử lý tay khoan nha khoa tại Bệnh viện Ung Bướu.'),
(N'QT.KSNK.17',3,N'Quyết định 3671/QĐ-BYT ngày 27/9/2012 của Bộ Y tế về Hướng dẫn khử khuẩn, tiệt khuẩn dụng cụ trong các cơ sở khám bệnh, chữa bệnh.
Quyết định 5991/QĐ-BYT ngày 26/12/2019 của Bộ Y tế về Hướng dẫn kiểm soát nhiễm khuẩn trong khám bệnh, chữa bệnh răng miệng.'),
(N'QT.KSNK.17',4,N'Tay khoan nha khoa: dụng cụ cơ học cầm tay dùng trong thủ thuật nha khoa. Tay khoan tốc độ cao hoạt động trên 180.000 vòng/phút; tay khoan tốc độ chậm hoạt động từ 600 đến 25.000 vòng/phút.
Tiệt khuẩn: quá trình tiêu diệt hoặc loại bỏ tất cả dạng vi sinh vật sống, bao gồm bào tử vi khuẩn.
Làm sạch/khử nhiễm: quá trình dùng biện pháp cơ học và hóa học để loại bỏ tác nhân nhiễm khuẩn và chất hữu cơ bám trên dụng cụ.
Từ viết tắt: ĐD, NV, NVYT, NB, KSNK, TKTT, DC, HC, PHCN theo PDF nguồn.'),
(N'QT.KSNK.17',8,N'5.2.1. Nguyên tắc: tay khoan phải được tiệt khuẩn giữa hai người bệnh; trang bị đủ số lượng theo ghế nha khoa; sử dụng dầu bôi trơn theo hướng dẫn nhà sản xuất; không ngâm trong dung dịch hoặc làm sạch bằng máy rửa siêu âm nếu nhà sản xuất không cho phép.

5.2.2. Chuẩn bị: chuẩn bị khăn sạch, bàn chải, hóa chất khử khuẩn bề mặt, dầu bôi trơn chuyên dụng và PHCN.

5.2.3. Làm sạch: cho tay khoan chạy không tải 10 - 15 giây, tháo mũi khoan, cọ rửa dưới vòi nước chảy, không ngâm nước, làm khô bên ngoài và thổi khô bên trong với tay khoan tốc độ cao.

5.2.4. Khử khuẩn: lau bên ngoài bằng khăn/giấy thấm hóa chất khử khuẩn bề mặt phù hợp, không ngâm tay khoan, xả lại, làm khô bên ngoài và bên trong.

5.2.5 - 5.2.7. Giao nhận, đóng gói, tiệt khuẩn, lưu trữ và bàn giao: khoa sử dụng giao tay khoan đã làm sạch/khử khuẩn cho KSNK; KSNK kiểm tra, bảo dưỡng/tra dầu nếu cần, đóng gói, tiệt khuẩn theo hướng dẫn nhà sản xuất, lưu kho vô khuẩn và bàn giao lại cho khoa sử dụng.'),
(N'QT.KSNK.17',10,N'BM.KSNK.17.01 Bảng kiểm xử lý tay khoan nha khoa; Phụ lục I Sổ giao nhận dụng cụ y tế theo PDF nguồn.');

UPDATE d
SET d.content_text = s.content
FROM med.procedure_document_sections d
JOIN med.procedure_versions v ON v.procedure_version_id = d.procedure_version_id
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
JOIN @sections s ON s.code = p.procedure_code AND s.section_order = d.section_order;

DECLARE @steps TABLE (
    code NVARCHAR(64),
    step_no INT,
    description NVARCHAR(MAX)
);

INSERT INTO @steps VALUES
(N'QT.KSNK.09',1,N'Sau sử dụng, dụng cụ được đưa về khu vực xử lý riêng; nhân viên mang phương tiện PHCN, pha hóa chất theo khuyến cáo, loại bỏ chất thải còn sót, tháo rời/mở khớp, ngâm enzyme đúng thời gian, chà rửa, tráng nước sạch và làm khô.'),
(N'QT.KSNK.09',2,N'ĐD dụng cụ kiểm tra dụng cụ sạch, khô; vận chuyển trong thùng có nắp đến khoa KSNK; hai bên kiểm đếm, ký sổ và ghi nhận hư hỏng, thất lạc hoặc nhu cầu khẩn cấp.'),
(N'QT.KSNK.09',3,N'NV KSNK kiểm tra độ sạch và số lượng, xử lý lại bằng máy rửa/khử khuẩn hoặc thao tác thủ công, tuân thủ hóa chất, thời gian, tráng và làm khô.'),
(N'QT.KSNK.09',4,N'Bảo dưỡng theo hướng dẫn nhà sản xuất; kiểm tra khớp, khóa, lòng ống, răng cưa, bề mặt, độ sắc bén, độ khô, gỉ sét và hư hỏng.'),
(N'QT.KSNK.09',5,N'Đóng gói bằng túi ép, hộp hoặc khay; đặt chỉ thị hóa học, hàn kín, ghi nhãn lô, ngày đóng gói, người đóng gói và hạn dùng.'),
(N'QT.KSNK.09',7,N'Theo dõi thông số chu trình, chỉ thị cơ học, hóa học, sinh học/PCD; ghi nhận kết quả, cách ly và xử lý lại mẻ không đạt.'),
(N'QT.KSNK.09',8,N'Lưu tại kho vô khuẩn, bảo đảm bao gói nguyên vẹn, khô sạch, đúng hạn dùng và nhập trước - xuất trước.'),
(N'QT.KSNK.09',9,N'Kiểm tra tình trạng vô khuẩn, nhãn, hạn dùng và số lượng trước khi giao; khoa GMHS tiếp nhận, ký sổ, trả lại dụng cụ không đạt.'),

(N'QT.KSNK.12',1,N'Khoa sử dụng làm sạch/khử khuẩn ban đầu: mang PHCN, pha hóa chất, tháo rời, ngâm enzyme, chà rửa, tráng, làm khô và khử khuẩn trung bình khi cần.'),
(N'QT.KSNK.12',2,N'Khoa sử dụng ghi số lượng/chủng loại, bàn giao trực tiếp cho KSNK; hai bên kiểm đếm, ký nhận và xử lý sai lệch.'),
(N'QT.KSNK.12',3,N'NV KSNK kiểm tra độ sạch, số lượng, chủng loại, xử lý bằng máy rửa khử khuẩn hoặc bằng tay tùy loại dụng cụ.'),
(N'QT.KSNK.12',4,N'Bảo dưỡng, kiểm tra chức năng, khóa khớp, lòng ống, vết bẩn, gỉ sét, biến dạng và độ khô; tách dụng cụ không đạt.'),
(N'QT.KSNK.12',5,N'Đóng gói bằng túi ép, hộp hoặc khay; đặt chỉ thị hóa học, hàn/niêm kín, ghi nhãn ngày đóng gói, người đóng gói, lô hấp và hạn dùng.'),
(N'QT.KSNK.12',7,N'Giám sát thông số mẻ, chỉ thị hóa học, sinh học/PCD; ghi nhận và cách ly mẻ không đạt.'),
(N'QT.KSNK.12',8,N'Bảo quản tại kho vô khuẩn, kiểm tra bao gói, nhãn, hạn dùng và sắp xếp nhập trước - xuất trước.'),
(N'QT.KSNK.12',9,N'Kiểm tra dụng cụ vô khuẩn trước khi giao; khoa sử dụng ký nhận và trả lại dụng cụ hư bao gói, quá hạn hoặc nghi nhiễm bẩn.'),

(N'QT.KSNK.16',1,N'Dụng cụ bán thiết yếu/hỗ trợ hô hấp sau sử dụng được xử lý tại khu riêng: mang PHCN, pha enzyme, loại bỏ chất thải, tháo rời, xả nước, ngâm, chà rửa, tráng và làm khô.'),
(N'QT.KSNK.16',2,N'Khoa sử dụng kiểm tra sạch/khô, ghi số lượng/chủng loại và bàn giao cho KSNK; KSNK kiểm đếm, ký nhận và yêu cầu xử lý lại nếu còn bẩn.'),
(N'QT.KSNK.16',3,N'Chuẩn bị bồn/khay ngâm, que thử nồng độ, đồng hồ, khăn vô khuẩn, máy sấy và hóa chất còn hạn; kiểm tra nồng độ, ngâm ngập đúng thời gian, tráng và làm khô.'),
(N'QT.KSNK.16',5,N'Lưu dụng cụ KKMĐC đã đóng gói tại kho vô khuẩn, duy trì bao gói khô, sạch, nguyên vẹn và đúng hạn sử dụng 14 ngày.'),
(N'QT.KSNK.16',6,N'NV kho cấp phát kiểm tra bao gói, hạn dùng và số lượng; khoa sử dụng tiếp nhận, ký sổ giao nhận và bảo quản trước khi dùng.'),

(N'QT.KSNK.17',1,N'Chuẩn bị khăn sạch, bàn chải, hóa chất khử khuẩn bề mặt, dầu bôi trơn chuyên dụng và PHCN.'),
(N'QT.KSNK.17',2,N'Cho tay khoan chạy không tải 10 - 15 giây, tháo mũi khoan, cọ rửa dưới vòi nước chảy, không ngâm nước, làm khô bên ngoài và thổi khô bên trong.'),
(N'QT.KSNK.17',3,N'Lau bên ngoài bằng khăn/giấy thấm hóa chất khử khuẩn bề mặt phù hợp, không ngâm tay khoan, xả lại, làm khô bên ngoài và bên trong.'),
(N'QT.KSNK.17',5,N'Khoa sử dụng chuyển tay khoan đã làm sạch/khử khuẩn cho KSNK, ghi số lượng và tình trạng vào sổ giao nhận; hai bên kiểm đếm và ký xác nhận.'),
(N'QT.KSNK.17',6,N'NV KSNK kiểm tra tay khoan khô, sạch, tra dầu/bảo dưỡng theo hướng dẫn nhà sản xuất nếu cần, đóng gói bằng vật liệu phù hợp và ghi nhãn.'),
(N'QT.KSNK.17',7,N'Tiệt khuẩn tay khoan theo hướng dẫn của nhà sản xuất, lựa chọn phương pháp và chu trình phù hợp; ghi nhận mẻ và kết quả giám sát.'),
(N'QT.KSNK.17',8,N'Lưu trữ tại kho vô khuẩn, bảo đảm bao gói nguyên vẹn, khô sạch, đúng nhãn và còn hạn sử dụng.'),
(N'QT.KSNK.17',9,N'Khoa KSNK kiểm tra số lượng, tình trạng vô khuẩn và bàn giao tay khoan cho khoa sử dụng; khoa sử dụng ký nhận và bảo quản đến khi dùng.');

UPDATE st
SET st.description = x.description
FROM med.procedure_steps st
JOIN med.procedure_versions v ON v.procedure_version_id = st.procedure_version_id
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
JOIN @steps x ON x.code = p.procedure_code AND x.step_no = st.step_no;

COMMIT TRANSACTION;
GO
