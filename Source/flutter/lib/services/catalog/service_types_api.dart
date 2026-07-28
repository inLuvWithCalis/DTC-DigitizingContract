import '../../models/catalog/service_type_dto.dart';
import '../api_client.dart';

class ServiceTypeApi {
  static final ApiClient _apiClient = ApiClient();
  static const String _baseUrl = '/catalog/service-types';

  /// Lấy danh sách loại dịch vụ có phân trang & tìm kiếm
  /// Mirrors: serviceTypeApi.getList(params: ServiceTypeFilterParams)
  static Future<PagedResult<ServiceTypeResponse>> getList(
    ServiceTypeFilterParams params,
  ) async {
    final queryParams = params.toQueryParameters();
    final queryString = Uri(queryParameters: queryParams).query;
    final endpoint = queryString.isNotEmpty ? '$_baseUrl?$queryString' : _baseUrl;

    final response = await _apiClient.get(endpoint);
    return PagedResult<ServiceTypeResponse>.fromJson(
      response as Map<String, dynamic>,
      (json) => ServiceTypeResponse.fromJson(json),
    );
  }

  /// Lấy chi tiết loại dịch vụ theo ID
  /// Mirrors: serviceTypeApi.getById(id: number)
  static Future<ServiceTypeResponse> getById(int id) async {
    final response = await _apiClient.get('$_baseUrl/$id');
    return ServiceTypeResponse.fromJson(response as Map<String, dynamic>);
  }

  /// Tạo loại dịch vụ mới
  /// Mirrors: serviceTypeApi.create(data: CreateServiceTypeRequest)
  static Future<ServiceTypeResponse> create(
    CreateServiceTypeRequest data,
  ) async {
    final response = await _apiClient.post(_baseUrl, body: data.toJson());
    return ServiceTypeResponse.fromJson(response as Map<String, dynamic>);
  }

  /// Cập nhật loại dịch vụ
  /// Mirrors: serviceTypeApi.update(id: number, data: UpdateServiceTypeRequest)
  static Future<void> update(
    int id,
    UpdateServiceTypeRequest data,
  ) async {
    await _apiClient.put('$_baseUrl/$id', body: data.toJson());
  }

  /// Xóa loại dịch vụ theo ID
  /// Mirrors: serviceTypeApi.delete(id: number)
  static Future<void> delete(int id) async {
    await _apiClient.delete('$_baseUrl/$id');
  }

  /// Xóa nhiều loại dịch vụ hàng loạt
  static Future<void> deleteBulk(List<int> ids) async {
    await Future.wait(ids.map((id) => delete(id)));
  }
}
