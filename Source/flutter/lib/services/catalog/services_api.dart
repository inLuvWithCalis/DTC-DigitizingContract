import '../../models/catalog/service_dto.dart';
import '../../models/catalog/service_type_dto.dart';
import '../api_client.dart';

class ServiceApi {
  static final ApiClient _apiClient = ApiClient();
  static const String _baseUrl = '/catalog/services';

  /// Lấy danh sách dịch vụ có phân trang, bộ lọc (status, serviceTypeId, fromDate, toDate) & tìm kiếm
  /// Mirrors: serviceApi.getList(params: ServiceFilterParams)
  static Future<PagedResult<ServiceResponse>> getList(
    ServiceFilterParams params,
  ) async {
    final queryParams = params.toQueryParameters();
    final queryString = Uri(queryParameters: queryParams).query;
    final endpoint = queryString.isNotEmpty ? '$_baseUrl?$queryString' : _baseUrl;

    final response = await _apiClient.get(endpoint);
    return PagedResult<ServiceResponse>.fromJson(
      response as Map<String, dynamic>,
      (json) => ServiceResponse.fromJson(json),
    );
  }

  /// Lấy chi tiết dịch vụ theo ID
  /// Mirrors: serviceApi.getById(id: number)
  static Future<ServiceResponse> getById(int id) async {
    final response = await _apiClient.get('$_baseUrl/$id');
    return ServiceResponse.fromJson(response as Map<String, dynamic>);
  }

  /// Tạo mới dịch vụ
  /// Mirrors: serviceApi.create(data: CreateServiceRequest)
  static Future<ServiceResponse> create(CreateServiceRequest data) async {
    final response = await _apiClient.post(_baseUrl, body: data.toJson());
    return ServiceResponse.fromJson(response as Map<String, dynamic>);
  }

  /// Cập nhật thông tin dịch vụ
  /// Mirrors: serviceApi.update(id: number, data: UpdateServiceRequest)
  static Future<void> update(int id, UpdateServiceRequest data) async {
    await _apiClient.put('$_baseUrl/$id', body: data.toJson());
  }

  /// Cập nhật trạng thái dịch vụ (Đang hoạt động / Ngừng hoạt động)
  /// Mirrors: serviceApi.setStatus(id: number, status: number)
  static Future<void> setStatus(int id, int status) async {
    await _apiClient.patch('$_baseUrl/$id/status?status=$status');
  }

  /// Xóa dịch vụ theo ID
  /// Mirrors: serviceApi.delete(id: number)
  static Future<void> delete(int id) async {
    await _apiClient.delete('$_baseUrl/$id');
  }

  /// Xóa nhiều dịch vụ hàng loạt
  static Future<void> deleteBulk(List<int> ids) async {
    await Future.wait(ids.map((id) => delete(id)));
  }
}
