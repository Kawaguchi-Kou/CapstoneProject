# POI & Advertisement Flow - Final Review Checklist

## 1) Final Business Rules (đã chốt)
- [ ] Phân loại POI theo `PartnerId`:
  - [ ] `PartnerId != null` => POI partner-owned
  - [ ] `PartnerId == null` => POI hệ thống
- [ ] POI hệ thống do Manager tạo, mặc định `Status = Active`
- [ ] Không gán `managerId` vào `PartnerId` của POI hệ thống
- [ ] Vòng đời `POIStatus`: `Pending`, `Rejected`, `Active`, `Inactive`
- [ ] State transition hợp lệ:
  - [ ] `Pending -> Active` (approve)
  - [ ] `Pending -> Rejected` (reject)
  - [ ] `Active -> Inactive` (inactivate)
  - [ ] `Inactive -> Active` (activate)
  - [ ] Không `Rejected -> Active` trực tiếp
  - [ ] Không `Active -> Rejected` trực tiếp
- [ ] Tạo Ads bắt buộc:
  - [ ] POI phải `Active`
  - [ ] Chỉ cho tạo Ads trên POI partner-owned (không cho POI hệ thống)
  - [ ] POI partner-owned phải đúng owner
  - [ ] Subscription check tại thời điểm khởi tạo Ads:
    - [ ] Có package đăng ký còn hạn
    - [ ] Còn quota để đăng Ads
    - [ ] Nếu chưa có package thì không cho tạo Ads

## 2) Authorization (đã chốt)
- [ ] Partner:
  - [ ] Tạo POI owned (set `PartnerId` từ token)
  - [ ] Sửa POI owned
  - [ ] Không xóa cứng POI; chỉ cho phép xóa mềm bằng `Inactive`
  - [ ] Được tự `inactivate` POI owned
  - [ ] Không được `activate` lại trực tiếp
- [ ] Manager/Staff:
  - [ ] Approve/Reject POI pending
  - [ ] Activate/Inactivate POI

## 3) Endpoints cần có
### Partner POI
- [ ] `POST /api/partner/pois`
- [ ] `GET /api/partner/pois/my`
- [ ] `GET /api/partner/pois/my/{id}`
- [ ] `PUT /api/partner/pois/my/{id}`
- [ ] `PATCH /api/partner/pois/my/{id}/inactivate`

### Manager/Staff moderation
- [ ] `GET /api/manager/pois/pending`
- [ ] `POST /api/manager/pois/{id}/approve`
- [ ] `POST /api/manager/pois/{id}/reject`
- [ ] `PATCH /api/manager/pois/{id}/inactivate`
- [ ] `PATCH /api/manager/pois/{id}/activate` (chỉ `Inactive -> Active`)

### Inactivate impact / confirm
- [ ] Có preview/cảnh báo số Ads bị ảnh hưởng trước khi inactivate
- [ ] Sau confirm mới thực thi cascade

## 4) Inactivate POI + Cascade Ads
- [ ] Khi inactivate POI có Ads `Active`:
  - [ ] Hiển thị cảnh báo cho người thao tác
  - [ ] Nếu confirm: POI `Active -> Inactive`
  - [ ] Cascade Ads liên quan `Active -> Inactive` (hoặc paused status tương đương)
- [ ] Chạy trong **1 transaction**
- [ ] Ads không được activate nếu POI vẫn `Inactive`

## 5) Database/Entity/Query
- [ ] `POIs.PartnerId` nullable FK đúng
- [ ] `POIs.Status` enum đúng
- [ ] Index: `PartnerId`, `Status`, (optional) `PartnerId + Status`
- [ ] Navigation nullable đúng (`Account? Partner`)
- [ ] Query pending: `PartnerId != null && Status == Pending`
- [ ] Query impact: đếm Ads active theo `POIId`

## 6) Service & Controller
- [ ] Validate ownership đầy đủ ở partner endpoints
- [ ] Validate transition hợp lệ trước update status
- [ ] Update `CreateAdvertisementAsync`:
  - [ ] Check POI tồn tại + Active + ownership
  - [ ] Giữ check subscription/quota hiện có
- [ ] Chuẩn hóa HTTP codes: `400/403/404/409`

## 7) Concurrency
- [ ] Dùng optimistic concurrency (`RowVersion`) cho update trạng thái POI
- [ ] Nếu race condition/ghi đè trạng thái => trả `409 Conflict`

## 8) Audit Log (khuyến nghị mạnh)
- [ ] Log action: approve/reject/activate/inactivate POI
- [ ] Log cascade Ads khi inactivate POI: `poiId`, `fromStatus`, `toStatus`, `affectedAdsCount`, `changedBy`, `timestamp`
- [ ] Log `reason` cho reject/inactivate (nếu có)

## 9) FE Readiness
- [ ] Popup confirm khi inactivate POI có Ads active
- [ ] Invalidate cache ads/pois sau thao tác status
- [ ] Form tạo Ads chỉ cho chọn POI hợp lệ (`Active` + ownership đúng)

## 10) Testing trước merge
- [ ] Unit: transition + ownership + ads validation
- [ ] Integration:
  - [ ] Partner tạo POI -> Manager approve -> tạo Ads thành công
  - [ ] Partner tạo POI -> Manager reject -> không tạo Ads được
  - [ ] Inactivate POI -> Ads active liên quan bị inactivate sau confirm
  - [ ] POI inactive -> Ads không thể active lại
- [ ] Regression test endpoint cũ không vỡ

## 11) Go-live
- [ ] Apply migration ở staging
- [ ] Smoke test luồng chính
- [ ] Verify data status/ownership
- [ ] Rollback plan sẵn sàng
