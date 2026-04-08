
using System.Net;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Requests;

using Application.DTOs.Responses;
using Application.Helper;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Application.Services
{
    public class POIService : IPOIService
    {
        private readonly IPOIRepository _poiRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGeocodingService _geocodingService;
        private readonly ILocationRepository _locationRepository;
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IPreferenceRepository _preferenceRepository;
        private readonly IDistrictRepository _districtRepository;

        public POIService(
            IPOIRepository poiRepository,
            IUserRepository userRepository,
            IGeocodingService geocodingService,
            ILocationRepository locationRepository,
            IAdvertisementRepository advertisementRepository,
            IPreferenceRepository preferenceRepository,
            IDistrictRepository districtRepository)
        {
            _poiRepository = poiRepository;
            _userRepository = userRepository;
            _geocodingService = geocodingService;
            _locationRepository = locationRepository;
            _advertisementRepository = advertisementRepository;
            _preferenceRepository = preferenceRepository;
            _districtRepository = districtRepository;
        }

        public async Task<List<RecommendedPoiResponse>> GetAllPoisSortedByPreferenceAsync(Guid accountId)
        {
            var userPrefs = await _userRepository.GetPreferenceByAccountIdAsync(accountId);
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            var userPrefSet = userPrefs
                .Select(x => x.Preference.Name)
                .ToHashSet();

            return pois
                .Select(poi =>
                {
                    var score = poi.PoiPreferences.Count(pp =>
                        pp.Preference != null &&
                        userPrefSet.Contains(pp.Preference.Name));

                    return new RecommendedPoiResponse
                    {
                        Id = poi.Id,
                        Name = poi.Name,
                        Address = poi.Address,
                        ApproxCost = poi.ApproxCost,
                        OpenHour = poi.OpenHour,
                        CloseHour = poi.CloseHour,
                        GoogleMapLink = poi.GoogleMapLink,
                        IsIndoor = poi.IsIndoor,
                        LocationName = poi.Location?.LocationName ?? "",
                        POIImgUrl = poi.POIImgUrl!,
                        Score = score,
                        POIPreferences = poi.PoiPreferences
                            .Where(pp => pp.Preference != null)
                            .Select(pp => pp.Preference.Name)
                            .ToList()
                    };
                })
                .OrderByDescending(x => x.Score)
                .ToList();
        }

        public async Task<List<POI>> GetAllAsync() 
        {
            return await _poiRepository.GetAllWithPreferencesAsync();  
        } 

        public async Task<POI?> GetByIdAsync(Guid id)
        {
            return await _poiRepository.GetByIdAsync(id);
        }

        public async Task<POI> CreateAsync(POI request, List<Guid> preferenceIds, Guid locationId, Guid districtId)
        {
            var (lat, lon) = await _geocodingService.GetCoordinatesAsync(request.Name, request.Location.LocationName);
            request.Latitude = lat;
            request.Longitude = lon;
            request.Status = POIStatus.Active;
            request.PartnerId = null;
            request.LocationId = locationId;
            request.DistrictId = districtId;

            await _poiRepository.AddAsync(request, preferenceIds);
            return request;
        }

        public async Task<POI> CreatePartnerPoiAsync(Guid partnerId, POI request, List<Guid> preferenceIds)
        {
            var (lat, lon) = await _geocodingService.GetCoordinatesAsync(request.Name, request.Location.LocationName);
            request.Latitude = lat;
            request.Longitude = lon;
            request.PartnerId = partnerId;
            request.Status = POIStatus.Pending;

            await _poiRepository.AddAsync(request, preferenceIds);
            return request;
        }

        public async Task<POI> UpdateAsync(Guid id, POI request)
        {
            var poi = await _poiRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("POI not found");

            if (!string.IsNullOrWhiteSpace(request.Address)) poi.Address = request.Address;
            if (!string.IsNullOrWhiteSpace(request.ApproxCost)) poi.ApproxCost = request.ApproxCost;
            if (!string.IsNullOrWhiteSpace(request.GoogleMapLink)) poi.GoogleMapLink = request.GoogleMapLink;
            poi.OpenHour = request.OpenHour;
            poi.CloseHour = request.CloseHour;
            poi.IsIndoor = request.IsIndoor;

            await _poiRepository.UpdateAsync(poi);
            return poi;
        }

        public async Task<POI> UpdatePartnerPoiAsync(Guid partnerId, Guid id, POI request)
        {
            var poi = await _poiRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("POI not found");

            if (poi.PartnerId != partnerId)
                throw new InvalidOperationException("Bạn không có quyền cập nhật POI này.");

            if (poi.Status == POIStatus.Inactive)
                throw new InvalidOperationException("POI đang inactive, không thể cập nhật.");

            if (!string.IsNullOrWhiteSpace(request.Name)) poi.Name = request.Name;
            if (!string.IsNullOrWhiteSpace(request.Address)) poi.Address = request.Address;
            if (!string.IsNullOrWhiteSpace(request.ApproxCost)) poi.ApproxCost = request.ApproxCost;
            if (!string.IsNullOrWhiteSpace(request.GoogleMapLink)) poi.GoogleMapLink = request.GoogleMapLink;
            if (!string.IsNullOrWhiteSpace(request.POIImgUrl)) poi.POIImgUrl = request.POIImgUrl;
            poi.OpenHour = request.OpenHour;
            poi.CloseHour = request.CloseHour;
            poi.IsIndoor = request.IsIndoor;

            if (poi.Status == POIStatus.Rejected)
                poi.Status = POIStatus.Pending;

            await _poiRepository.UpdateAsync(poi);
            return poi;
        }

        public async Task<List<POI>> GetMyPoisAsync(Guid partnerId)
        {
            return await _poiRepository.GetByPartnerIdAsync(partnerId);
        } 

        public async Task<PagedResultResponse<POI>> GetMyPoisAsync(Guid partnerId, int page, int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var skip = (page - 1) * pageSize;
            var totalItems = await _poiRepository.CountByPartnerIdAsync(partnerId);
            var items = await _poiRepository.GetByPartnerIdAsync(partnerId, skip, pageSize);

            return new PagedResultResponse<POI>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        public async Task<POI?> GetMyPoiByIdAsync(Guid partnerId, Guid poiId)
        {
            var poi = await _poiRepository.GetByIdAsync(poiId);
            return poi?.PartnerId == partnerId ? poi : null;
        }

        public async Task<List<POI>> GetPendingPartnerPoisAsync() => await _poiRepository.GetPendingPartnerPoisAsync();

        public async Task<PagedResultResponse<POI>> GetPendingPartnerPoisAsync(int page, int pageSize)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var skip = (page - 1) * pageSize;
            var totalItems = await _poiRepository.CountPendingPartnerPoisAsync();
            var items = await _poiRepository.GetPendingPartnerPoisAsync(skip, pageSize);

            return new PagedResultResponse<POI>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        public async Task<POI> ApprovePartnerPoiAsync(Guid poiId)
        {
            var poi = await _poiRepository.GetByIdAsync(poiId) ?? throw new KeyNotFoundException("POI not found");
            if (poi.PartnerId == null) throw new InvalidOperationException("POI hệ thống không thuộc luồng duyệt partner.");
            if (poi.Status != POIStatus.Pending) throw new InvalidOperationException("Chỉ POI Pending mới được duyệt.");

            poi.Status = POIStatus.Active;
            await _poiRepository.UpdateAsync(poi);
            return poi;
        }

        public async Task<POI> RejectPartnerPoiAsync(Guid poiId)
        {
            var poi = await _poiRepository.GetByIdAsync(poiId) ?? throw new KeyNotFoundException("POI not found");
            if (poi.PartnerId == null) throw new InvalidOperationException("POI hệ thống không thuộc luồng duyệt partner.");
            if (poi.Status != POIStatus.Pending) throw new InvalidOperationException("Chỉ POI Pending mới được từ chối.");

            poi.Status = POIStatus.Rejected;
            await _poiRepository.UpdateAsync(poi);
            return poi;
        }

        public async Task<(POI poi, int affectedAds)> InactivatePoiAsync(Guid actorId, Guid poiId, bool isManagerOrStaff, bool confirmCascade)
        {
            var poi = await _poiRepository.GetByIdAsync(poiId) ?? throw new KeyNotFoundException("POI not found");

            if (!isManagerOrStaff)
            {
                if (poi.PartnerId == null || poi.PartnerId != actorId)
                    throw new InvalidOperationException("Bạn không có quyền inactivate POI này.");
            }

            if (poi.Status != POIStatus.Active)
                throw new InvalidOperationException("Chỉ POI Active mới có thể chuyển Inactive.");

            var activeAdsCount = await _advertisementRepository.CountActiveByPoiIdAsync(poiId);
            if (activeAdsCount > 0 && !confirmCascade)
                throw new InvalidOperationException($"POI này đang có {activeAdsCount} ads active. Vui lòng xác nhận cascade để tiếp tục.");

            poi.Status = POIStatus.Inactive;
            await _poiRepository.UpdateAsync(poi);

            var affectedAds = 0;
            if (activeAdsCount > 0)
            {
                affectedAds = await _advertisementRepository.InactivateActiveByPoiIdAsync(poiId);
            }

            return (poi, affectedAds);
        }

        public async Task<POI> ActivatePoiAsync(Guid poiId)
        {
            var poi = await _poiRepository.GetByIdAsync(poiId) ?? throw new KeyNotFoundException("POI not found");

            if (poi.Status != POIStatus.Inactive)
                throw new InvalidOperationException("Chỉ POI Inactive mới có thể chuyển Active.");

            poi.Status = POIStatus.Active;
            await _poiRepository.UpdateAsync(poi);
            return poi;
        }

        //public async Task ImportExcelAsync(IFormFile file)
        //{
        //    using var stream = new MemoryStream();
        //    await file.CopyToAsync(stream);

        //    using var package = new ExcelPackage(stream);
        //    var worksheet = package.Workbook.Worksheets[0];
        //    int rowCount = worksheet.Dimension.Rows;

        //    // 🔥 Load toàn bộ Location 1 lần (tránh gọi DB trong loop)
        //    var locations = (await _locationRepository.GetAllAsync())
        //      .GroupBy(x => x.LocationName.ToLower())
        //      .ToDictionary(g => g.Key, g => g.First());

        //    List<POI> pois = new();

        //    for (int row = 2; row <= rowCount; row++)
        //    {
        //        string name = worksheet.Cells[row, 1].Text.Trim();
        //        string address = worksheet.Cells[row, 2].Text.Trim();
        //        string cityRaw = worksheet.Cells[row, 3].Text.Trim();
        //        var prefRaw = worksheet.Cells[row, 8].Text.Trim();
        //        var preferenceIds = new List<Guid>();
        //        if (!string.IsNullOrEmpty(prefRaw))
        //        {
        //            preferenceIds = prefRaw
        //                .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //                .Select(x => Guid.TryParse(x.Trim(), out var id) ? id : (Guid?)null)
        //                .Where(x => x.HasValue)
        //                .Select(x => x!.Value)
        //                .ToList();
        //        }


        //        string cityKey = cityRaw.ToLower();

        //        if (!locations.ContainsKey(cityKey))
        //            continue;

        //        var location = locations[cityKey];

        //        decimal.TryParse(worksheet.Cells[row, 4].Text, out var cost);
        //        bool.TryParse(worksheet.Cells[row, 7].Text, out var isIndoor);

        //        //Opening Hours Parsing
        //        var openingRaw = worksheet.Cells[row, 5].Text.Trim();

        //        TimeOnly? openHour = null;
        //        TimeOnly? closeHour = null;
        //        bool is24Hours = false;

        //        if (!string.IsNullOrWhiteSpace(openingRaw) && openingRaw.Contains("~"))
        //        {
        //            var parts = openingRaw.Split('~', StringSplitOptions.TrimEntries);

        //            if (parts.Length == 2)
        //            {
        //                if (TimeOnly.TryParse(parts[0], out var open))
        //                    openHour = open;

        //                if (TimeOnly.TryParse(parts[1], out var close))
        //                    closeHour = close;

        //                // 24h case
        //                if (openHour == TimeOnly.MinValue && closeHour == TimeOnly.MinValue)
        //                {
        //                    is24Hours = true;
        //                }
        //            }
        //        }

        //        // Visit Recommendation
        //        string visitRecommendation = GetVisitRecommendation(
        //            openHour,
        //            closeHour,
        //            is24Hours,
        //            isIndoor
        //        );

        //        // LẤY IMAGE TỪ MAP
        //        var normalizedName = name.Trim().ToLower();

        //        string? imageUrl = _imageMap.ContainsKey(normalizedName)
        //            ? _imageMap[normalizedName]
        //            : null;

        //        var poi = new POI
        //        {
        //            Id = Guid.NewGuid(),

        //            Name = name,
        //            Address = address,
        //            City = cityRaw,

        //            ApproxCost = cost.ToString(),
        //            OpenHour = openHour,
        //            CloseHour = closeHour,
        //            Is24Hours = is24Hours,
        //            VisitRecommendation = visitRecommendation,
        //            GoogleMapLink = worksheet.Cells[row, 6].Text,
        //            IsIndoor = isIndoor,

        //            LocationId = location.LocationId,
        //            Latitude = location.Latitude,
        //            Longitude = location.Longitude,

        //            POIImgUrl = imageUrl
        //        };
        //        var poiPreferences = preferenceIds.Select(prefId => new POIPreference
        //        {
        //            PoiId = poi.Id,
        //            PreferenceId = prefId
        //        }).ToList();

        //        pois.Add(poi);
        //    }


        //    await _poiRepository.AddRangeAsync(pois);
        //}

        public async Task ImportExcelAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            // =========================
            // LOAD MASTER DATA
            // =========================

            var locations = (await _locationRepository.GetAllAsync())
                .GroupBy(x => StringNormalizer.Normalize(x.LocationName))
                .ToDictionary(g => g.Key, g => g.First());

            var districts = (await _districtRepository.GetAllAsync())
                .GroupBy(d => new
                {
                    Name = StringNormalizer.Normalize(d.Name),
                    d.LocationId
                })
                .ToDictionary(g => (g.Key.Name, g.Key.LocationId), g => g.First());

            var preferenceMap = (await _preferenceRepository.GetAllAsync())
                .ToDictionary(x => x.Name.ToLower(), x => x.Id);

            List<POI> pois = new();

            // =========================
            // LOOP ROWS
            // =========================

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    // ===== BASIC =====
                    string name = worksheet.Cells[row, 1].Text.Trim();
                    string address = worksheet.Cells[row, 2].Text.Trim();
                    string cityRaw = worksheet.Cells[row, 3].Text.Trim();
                    string districtRaw = worksheet.Cells[row, 4].Text.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                        throw new Exception("Name is empty");

                    // ===== LOCATION =====
                    string cityKey = StringNormalizer.Normalize(cityRaw);

                    if (!locations.TryGetValue(cityKey, out var location))
                        throw new Exception($"Location not found '{cityRaw}'");

                    // ===== DISTRICT =====
                    string districtKey = StringNormalizer.Normalize(districtRaw);
                    var districtLookupKey = (districtKey, location.LocationId);

                    if (!districts.TryGetValue(districtLookupKey, out var district))
                        throw new Exception($"District '{districtRaw}' not found in '{cityRaw}'");

                    // ===== COST =====
                    if (!decimal.TryParse(worksheet.Cells[row, 5].Text, out var cost))
                        throw new Exception("Invalid cost");

                    // ===== OPENING =====
                    var openingRaw = worksheet.Cells[row, 6].Text.Trim();

                    TimeOnly? openHour = null;
                    TimeOnly? closeHour = null;
                    bool is24Hours = false;

                    if (!string.IsNullOrWhiteSpace(openingRaw))
                    {
                        var parts = openingRaw.Split('~', StringSplitOptions.TrimEntries);

                        if (parts.Length != 2)
                            throw new Exception("Invalid opening format (use HH:mm~HH:mm)");

                        if (!TimeOnly.TryParse(parts[0], out var open))
                            throw new Exception("Invalid open hour");

                        if (!TimeOnly.TryParse(parts[1], out var close))
                            throw new Exception("Invalid close hour");

                        openHour = open;
                        closeHour = close;

                        if (open == TimeOnly.MinValue && close == TimeOnly.MinValue)
                            is24Hours = true;
                    }

                    // ===== GOOGLE MAP =====
                    string googleMap = worksheet.Cells[row, 7].Text.Trim();

                    // ===== INDOOR =====
                    if (!bool.TryParse(worksheet.Cells[row, 8].Text, out var isIndoor))
                        throw new Exception("Invalid Indoor value");

                    // ===== PREFERENCES =====
                    var prefRaw = worksheet.Cells[row, 9].Text.Trim();
                    var preferenceIds = new List<Guid>();

                    if (!string.IsNullOrEmpty(prefRaw))
                    {
                        var prefNames = prefRaw.Split(',', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var pref in prefNames)
                        {
                            var key = pref.Trim().ToLower();

                            if (!preferenceMap.TryGetValue(key, out var prefId))
                                throw new Exception($"Preference '{pref}' not found");

                            preferenceIds.Add(prefId);
                        }
                    }

                    // ===== TYPE =====
                    var typeRaw = worksheet.Cells[row, 10].Text.Trim();

                    if (!Enum.TryParse<POIType>(typeRaw, true, out var poiType))
                        throw new Exception($"Invalid POIType '{typeRaw}'");

                    // ===== LAT LNG =====
                    if (!double.TryParse(worksheet.Cells[row, 11].Text, out var lat))
                        throw new Exception("Invalid Latitude");

                    if (!double.TryParse(worksheet.Cells[row, 12].Text, out var lng))
                        throw new Exception("Invalid Longitude");

                    // ===== IMAGE =====
                    var normalizedName = name.ToLower();
                    string? imageUrl = _imageMap.TryGetValue(normalizedName, out var img)
                        ? img
                        : null;

                    // ===== CREATE POI =====
                    var poi = new POI
                    {
                        Id = Guid.NewGuid(),

                        Name = name,
                        Address = $"{address}, {district.Name}, {location.LocationName}",

                        ApproxCost = cost.ToString(),
                        OpenHour = openHour,
                        CloseHour = closeHour,
                        Is24Hours = is24Hours,

                        Type = poiType,

                        VisitRecommendation = GetVisitRecommendation(
                            openHour, closeHour, is24Hours, isIndoor),

                        GoogleMapLink = googleMap,
                        IsIndoor = isIndoor,

                        LocationId = location.LocationId,
                        DistrictId = district.Id, // 🔥 FIXED

                        Latitude = lat,
                        Longitude = lng,

                        Status = POIStatus.Active,
                        POIImgUrl = imageUrl
                    };

                    // ===== PREFERENCES RELATION =====
                    poi.PoiPreferences = preferenceIds.Select(prefId => new POIPreference
                    {
                        PoiId = poi.Id,
                        PreferenceId = prefId
                    }).ToList();

                    pois.Add(poi);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Row {row}: {ex.Message}");
                }
            }

            // =========================
            // SAVE
            // =========================
            await _poiRepository.AddRangeAsync(pois);
        }

        private static readonly Dictionary<string, string> _imageMap = new();

        public void AddImageMapping(string fileName, string url)
        {
            var key = Path.GetFileNameWithoutExtension(fileName).Trim().ToLower();
            _imageMap[key] = url;
        }
        public async Task<byte[]> ExportExcelAsync()
        {
            var pois = await _poiRepository.GetAllWithPreferencesAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("POIs");

            // Headers
            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Address";
            worksheet.Cells[1, 3].Value = "City";
            worksheet.Cells[1, 4].Value = "ApproxCost";
            worksheet.Cells[1, 5].Value = "Opening Hours";
            worksheet.Cells[1, 6].Value = "GoogleMapLink";
            worksheet.Cells[1, 7].Value = "IsIndoor";
            worksheet.Cells[1, 8].Value = "Preferences";
            worksheet.Cells[1, 9].Value = "Type";
            worksheet.Cells[1, 10].Value = "Latitude";
            worksheet.Cells[1, 11].Value = "Longitude";

            // Header styling
            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < pois.Count; i++)
            {
                var poi = pois[i];
                int row = i + 2;

                worksheet.Cells[row, 1].Value = poi.Name;
                worksheet.Cells[row, 2].Value = poi.Address;
                worksheet.Cells[row, 3].Value = "";
                worksheet.Cells[row, 4].Value = poi.ApproxCost;

                string openHours = string.Empty;
                if (poi.OpenHour.HasValue && poi.CloseHour.HasValue)
                {
                    openHours = $"{poi.OpenHour.Value.ToString("HH:mm")} ~ {poi.CloseHour.Value.ToString("HH:mm")}";
                }
                else if (poi.Is24Hours)
                {
                    openHours = "00:00 ~ 00:00";
                }
                worksheet.Cells[row, 5].Value = openHours;

                worksheet.Cells[row, 6].Value = poi.GoogleMapLink;
                worksheet.Cells[row, 7].Value = poi.IsIndoor ? "TRUE" : "FALSE";

                var prefs = string.Empty;
                if (poi.PoiPreferences != null)
                {
                    var prefNames = poi.PoiPreferences
                        .Where(pp => pp.Preference != null)
                        .Select(pp => pp.Preference.Name)
                        .ToList();
                    prefs = string.Join(", ", prefNames);
                }
                worksheet.Cells[row, 8].Value = prefs;

                worksheet.Cells[row, 9].Value = poi.Type.ToString();
                worksheet.Cells[row, 10].Value = poi.Latitude;
                worksheet.Cells[row, 11].Value = poi.Longitude;
            }

            worksheet.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }

        private string GetVisitRecommendation(TimeOnly? openHour, TimeOnly? closeHour, bool is24Hours, bool isIndoor)
        {
            if (is24Hours) return "Open 24 hours - can be visited anytime";
            if (!openHour.HasValue || !closeHour.HasValue) return "Opening hours not available";

            var open = openHour.Value;
            var close = closeHour.Value;

            if (open <= new TimeOnly(6, 0) && close <= new TimeOnly(14, 0)) return "Best visited in the morning";
            if (open >= new TimeOnly(10, 0) && close <= new TimeOnly(18, 0)) return "Best visited in the afternoon";
            if (open >= new TimeOnly(16, 0) || close >= new TimeOnly(22, 0)) return "Ideal for evening or night visits";
            if (!isIndoor) return "Best visited during daylight hours";
            return "Suitable to visit at any time of the day";
        }
    }
}
