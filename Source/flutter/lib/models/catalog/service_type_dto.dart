class ServiceTypeFilterParams {
  final int page;
  final int pageSize;
  final String? keyword;
  final int? langId;

  ServiceTypeFilterParams({
    this.page = 1,
    this.pageSize = 10,
    this.keyword,
    this.langId,
  });

  Map<String, String> toQueryParameters() {
    final params = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (keyword != null && keyword!.trim().isNotEmpty) {
      params['keyword'] = keyword!.trim();
    }
    if (langId != null) {
      params['langId'] = langId!.toString();
    }
    return params;
  }
}

class CreateServiceTypeRequest {
  final String serviceTypeName;
  final int? langId;

  CreateServiceTypeRequest({
    required this.serviceTypeName,
    this.langId,
  });

  Map<String, dynamic> toJson() {
    return {
      'serviceTypeName': serviceTypeName,
      if (langId != null) 'langId': langId,
    };
  }
}

class UpdateServiceTypeRequest {
  final String serviceTypeName;
  final int? langId;

  UpdateServiceTypeRequest({
    required this.serviceTypeName,
    this.langId,
  });

  Map<String, dynamic> toJson() {
    return {
      'serviceTypeName': serviceTypeName,
      if (langId != null) 'langId': langId,
    };
  }
}

class ServiceTypeResponse {
  final int serviceTypeId;
  final String? serviceTypeName;
  final int? langId;
  final int serviceCount;

  ServiceTypeResponse({
    required this.serviceTypeId,
    this.serviceTypeName,
    this.langId,
    required this.serviceCount,
  });

  factory ServiceTypeResponse.fromJson(Map<String, dynamic> json) {
    int parseToInt(dynamic val) {
      if (val is int) return val;
      if (val != null) return int.tryParse(val.toString()) ?? 0;
      return 0;
    }

    int? parseToNullableInt(dynamic val) {
      if (val is int) return val;
      if (val != null) return int.tryParse(val.toString());
      return null;
    }

    return ServiceTypeResponse(
      serviceTypeId: parseToInt(json['serviceTypeId']),
      serviceTypeName: json['serviceTypeName']?.toString(),
      langId: parseToNullableInt(json['langId']),
      serviceCount: parseToInt(json['serviceCount']),
    );
  }
}

class PagedResult<T> {
  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;

  PagedResult({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
  });

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromJsonT,
  ) {
    int parseToInt(dynamic val) {
      if (val is int) return val;
      if (val != null) return int.tryParse(val.toString()) ?? 0;
      return 0;
    }

    final rawItems = json['items'] as List<dynamic>? ?? [];
    final items = rawItems
        .map((item) => fromJsonT(item as Map<String, dynamic>))
        .toList();

    return PagedResult<T>(
      items: items,
      totalCount: parseToInt(json['totalCount']),
      page: parseToInt(json['page']),
      pageSize: parseToInt(json['pageSize']),
      totalPages: parseToInt(json['totalPages']),
    );
  }
}
