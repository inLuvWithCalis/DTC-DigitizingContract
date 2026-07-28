enum ServiceStatus {
  active(1),
  inactive(0);

  final int value;
  const ServiceStatus(this.value);
}

String getServiceStatusLabel(int? status) {
  switch (status) {
    case 1:
      return "Đang hoạt động";
    case 0:
      return "Ngừng hoạt động";
    default:
      return "Chưa cập nhật";
  }
}

class ServiceFilterParams {
  final int page;
  final int pageSize;
  final String? keyword;
  final int? serviceTypeId;
  final int? status;
  final int? langId;
  final String? fromDate;
  final String? toDate;

  ServiceFilterParams({
    this.page = 1,
    this.pageSize = 10,
    this.keyword,
    this.serviceTypeId,
    this.status,
    this.langId,
    this.fromDate,
    this.toDate,
  });

  Map<String, String> toQueryParameters() {
    final params = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (keyword != null && keyword!.trim().isNotEmpty) {
      params['keyword'] = keyword!.trim();
    }
    if (serviceTypeId != null) {
      params['serviceTypeId'] = serviceTypeId!.toString();
    }
    if (status != null) {
      params['status'] = status!.toString();
    }
    if (langId != null) {
      params['langId'] = langId!.toString();
    }
    if (fromDate != null && fromDate!.trim().isNotEmpty) {
      params['fromDate'] = fromDate!.trim();
    }
    if (toDate != null && toDate!.trim().isNotEmpty) {
      params['toDate'] = toDate!.trim();
    }
    return params;
  }
}

class CreateServiceRequest {
  final String serviceName;
  final int? serviceTypeId;
  final int? serviceParentId;
  final double? servicePrice;
  final double? setupPrice;
  final double? maintainPrice;
  final int? langId;
  final String? serviceImageIcon;
  final String? serviceShortDesc;
  final String? serviceContent;
  final int? serviceOrder;
  final int? serviceRegion;
  final String? rewrite;
  final String? titleBrowser;
  final String? metaKeyword;
  final String? metaDescription;
  final String? others;

  CreateServiceRequest({
    required this.serviceName,
    this.serviceTypeId,
    this.serviceParentId,
    this.servicePrice,
    this.setupPrice,
    this.maintainPrice,
    this.langId,
    this.serviceImageIcon,
    this.serviceShortDesc,
    this.serviceContent,
    this.serviceOrder,
    this.serviceRegion,
    this.rewrite,
    this.titleBrowser,
    this.metaKeyword,
    this.metaDescription,
    this.others,
  });

  Map<String, dynamic> toJson() {
    return {
      'serviceName': serviceName,
      if (serviceTypeId != null) 'serviceTypeId': serviceTypeId,
      if (serviceParentId != null) 'serviceParentId': serviceParentId,
      if (servicePrice != null) 'servicePrice': servicePrice,
      if (setupPrice != null) 'setupPrice': setupPrice,
      if (maintainPrice != null) 'maintainPrice': maintainPrice,
      if (langId != null) 'langId': langId,
      if (serviceImageIcon != null) 'serviceImageIcon': serviceImageIcon,
      if (serviceShortDesc != null) 'serviceShortDesc': serviceShortDesc,
      if (serviceContent != null) 'serviceContent': serviceContent,
      if (serviceOrder != null) 'serviceOrder': serviceOrder,
      if (serviceRegion != null) 'serviceRegion': serviceRegion,
      if (rewrite != null) 'rewrite': rewrite,
      if (titleBrowser != null) 'titleBrowser': titleBrowser,
      if (metaKeyword != null) 'metaKeyword': metaKeyword,
      if (metaDescription != null) 'metaDescription': metaDescription,
      if (others != null) 'others': others,
    };
  }
}

class UpdateServiceRequest {
  final String serviceName;
  final int? serviceTypeId;
  final int? serviceParentId;
  final double? servicePrice;
  final double? setupPrice;
  final double? maintainPrice;
  final int? langId;
  final String? serviceImageIcon;
  final String? serviceShortDesc;
  final String? serviceContent;
  final int? serviceOrder;
  final int? serviceRegion;
  final String? rewrite;
  final String? titleBrowser;
  final String? metaKeyword;
  final String? metaDescription;
  final String? others;

  UpdateServiceRequest({
    required this.serviceName,
    this.serviceTypeId,
    this.serviceParentId,
    this.servicePrice,
    this.setupPrice,
    this.maintainPrice,
    this.langId,
    this.serviceImageIcon,
    this.serviceShortDesc,
    this.serviceContent,
    this.serviceOrder,
    this.serviceRegion,
    this.rewrite,
    this.titleBrowser,
    this.metaKeyword,
    this.metaDescription,
    this.others,
  });

  Map<String, dynamic> toJson() {
    return {
      'serviceName': serviceName,
      if (serviceTypeId != null) 'serviceTypeId': serviceTypeId,
      if (serviceParentId != null) 'serviceParentId': serviceParentId,
      if (servicePrice != null) 'servicePrice': servicePrice,
      if (setupPrice != null) 'setupPrice': setupPrice,
      if (maintainPrice != null) 'maintainPrice': maintainPrice,
      if (langId != null) 'langId': langId,
      if (serviceImageIcon != null) 'serviceImageIcon': serviceImageIcon,
      if (serviceShortDesc != null) 'serviceShortDesc': serviceShortDesc,
      if (serviceContent != null) 'serviceContent': serviceContent,
      if (serviceOrder != null) 'serviceOrder': serviceOrder,
      if (serviceRegion != null) 'serviceRegion': serviceRegion,
      if (rewrite != null) 'rewrite': rewrite,
      if (titleBrowser != null) 'titleBrowser': titleBrowser,
      if (metaKeyword != null) 'metaKeyword': metaKeyword,
      if (metaDescription != null) 'metaDescription': metaDescription,
      if (others != null) 'others': others,
    };
  }
}

class ServiceResponse {
  final int serviceId;
  final String? serviceName;
  final int? serviceTypeId;
  final String? serviceTypeName;
  final int? serviceParentId;
  final double? servicePrice;
  final double? setupPrice;
  final double? maintainPrice;
  final int? status;
  final int? langId;
  final String? serviceImageIcon;
  final String? serviceShortDesc;
  final String? serviceContent;
  final int? serviceOrder;
  final int? serviceRegion;
  final String? rewrite;
  final String? titleBrowser;
  final String? metaKeyword;
  final String? metaDescription;
  final String? others;
  final int? userCreated;
  final int? userModified;
  final String? dateCreated;
  final String? dateModified;

  ServiceResponse({
    required this.serviceId,
    this.serviceName,
    this.serviceTypeId,
    this.serviceTypeName,
    this.serviceParentId,
    this.servicePrice,
    this.setupPrice,
    this.maintainPrice,
    this.status,
    this.langId,
    this.serviceImageIcon,
    this.serviceShortDesc,
    this.serviceContent,
    this.serviceOrder,
    this.serviceRegion,
    this.rewrite,
    this.titleBrowser,
    this.metaKeyword,
    this.metaDescription,
    this.others,
    this.userCreated,
    this.userModified,
    this.dateCreated,
    this.dateModified,
  });

  factory ServiceResponse.fromJson(Map<String, dynamic> json) {
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

    double? parseToNullableDouble(dynamic val) {
      if (val is double) return val;
      if (val is int) return val.toDouble();
      if (val != null) return double.tryParse(val.toString());
      return null;
    }

    return ServiceResponse(
      serviceId: parseToInt(json['serviceId']),
      serviceName: json['serviceName']?.toString(),
      serviceTypeId: parseToNullableInt(json['serviceTypeId']),
      serviceTypeName: json['serviceTypeName']?.toString(),
      serviceParentId: parseToNullableInt(json['serviceParentId']),
      servicePrice: parseToNullableDouble(json['servicePrice']),
      setupPrice: parseToNullableDouble(json['setupPrice']),
      maintainPrice: parseToNullableDouble(json['maintainPrice']),
      status: parseToNullableInt(json['status']),
      langId: parseToNullableInt(json['langId']),
      serviceImageIcon: json['serviceImageIcon']?.toString(),
      serviceShortDesc: json['serviceShortDesc']?.toString(),
      serviceContent: json['serviceContent']?.toString(),
      serviceOrder: parseToNullableInt(json['serviceOrder']),
      serviceRegion: parseToNullableInt(json['serviceRegion']),
      rewrite: json['rewrite']?.toString(),
      titleBrowser: json['titleBrowser']?.toString(),
      metaKeyword: json['metaKeyword']?.toString(),
      metaDescription: json['metaDescription']?.toString(),
      others: json['others']?.toString(),
      userCreated: parseToNullableInt(json['userCreated']),
      userModified: parseToNullableInt(json['userModified']),
      dateCreated: json['dateCreated']?.toString(),
      dateModified: json['dateModified']?.toString(),
    );
  }
}
