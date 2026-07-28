class LoginRequestDto {
  final String accountName;
  final String? password;

  LoginRequestDto({
    required this.accountName,
    this.password,
  });

  Map<String, dynamic> toJson() {
    return {
      'accountName': accountName,
      if (password != null) 'password': password,
    };
  }
}

class LoginResponseDto {
  final String message;
  final int employeeId;
  final String? employeeName;
  final int? tenantId;
  final String? tenantCode;
  final String? tenantName;

  LoginResponseDto({
    required this.message,
    required this.employeeId,
    this.employeeName,
    this.tenantId,
    this.tenantCode,
    this.tenantName,
  });

  factory LoginResponseDto.fromJson(Map<String, dynamic> json) {
    return LoginResponseDto(
      message: json['message']?.toString() ?? '',
      employeeId: json['employeeId'] is int
          ? json['employeeId'] as int
          : int.tryParse(json['employeeId']?.toString() ?? '0') ?? 0,
      employeeName: json['employeeName']?.toString(),
      tenantId: json['tenantId'] is int
          ? json['tenantId'] as int
          : (json['tenantId'] != null
              ? int.tryParse(json['tenantId'].toString())
              : null),
      tenantCode: json['tenantCode']?.toString(),
      tenantName: json['tenantName']?.toString(),
    );
  }
}

class UserProfileDto {
  final int employeeId;
  final String? employeeCode;
  final String employeeAccount;
  final String? employeeFullName;
  final int? titleId;
  final String? employeeBirthDate;
  final int? maritalStatus;
  final int? gender;
  final String? employeeMobile;
  final String? employeePhone;
  final String? employeeEmail;
  final String? employeeAddress;
  final int? userCreated;
  final int? userModified;
  final String? dateCreated;
  final String? dateModified;
  final String? hireDate;
  final int? status;
  final int? departmentId;
  final String? others;
  final String? defaultPage;
  final String? employeeImageIcon;
  final int? employeeType;
  final String? userRoles;
  final int? workTypeId;

  UserProfileDto({
    required this.employeeId,
    this.employeeCode,
    required this.employeeAccount,
    this.employeeFullName,
    this.titleId,
    this.employeeBirthDate,
    this.maritalStatus,
    this.gender,
    this.employeeMobile,
    this.employeePhone,
    this.employeeEmail,
    this.employeeAddress,
    this.userCreated,
    this.userModified,
    this.dateCreated,
    this.dateModified,
    this.hireDate,
    this.status,
    this.departmentId,
    this.others,
    this.defaultPage,
    this.employeeImageIcon,
    this.employeeType,
    this.userRoles,
    this.workTypeId,
  });

  factory UserProfileDto.fromJson(Map<String, dynamic> json) {
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

    return UserProfileDto(
      employeeId: parseToInt(json['employeeId']),
      employeeCode: json['employeeCode']?.toString(),
      employeeAccount: json['employeeAccount']?.toString() ?? '',
      employeeFullName: json['employeeFullName']?.toString(),
      titleId: parseToNullableInt(json['titleId']),
      employeeBirthDate: json['employeeBirthDate']?.toString(),
      maritalStatus: parseToNullableInt(json['maritalStatus']),
      gender: parseToNullableInt(json['gender']),
      employeeMobile: json['employeeMobile']?.toString(),
      employeePhone: json['employeePhone']?.toString(),
      employeeEmail: json['employeeEmail']?.toString(),
      employeeAddress: json['employeeAddress']?.toString(),
      userCreated: parseToNullableInt(json['userCreated']),
      userModified: parseToNullableInt(json['userModified']),
      dateCreated: json['dateCreated']?.toString(),
      dateModified: json['dateModified']?.toString(),
      hireDate: json['hireDate']?.toString(),
      status: parseToNullableInt(json['status']),
      departmentId: parseToNullableInt(json['departmentId']),
      others: json['others']?.toString(),
      defaultPage: json['defaultPage']?.toString(),
      employeeImageIcon: json['employeeImageIcon']?.toString(),
      employeeType: parseToNullableInt(json['employeeType']),
      userRoles: json['userRoles']?.toString(),
      workTypeId: parseToNullableInt(json['workTypeId']),
    );
  }
}
