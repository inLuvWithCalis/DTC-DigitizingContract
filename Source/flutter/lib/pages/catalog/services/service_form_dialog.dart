import 'package:flutter/material.dart';
import '../../../models/catalog/service_dto.dart';
import '../../../models/catalog/service_type_dto.dart';
import '../../../services/catalog/service_types_api.dart';
import '../../../services/catalog/services_api.dart';
import '../../../utils/app_toast.dart';
import '../../../widgets/app_text_field.dart';

class ServiceFormDialog extends StatefulWidget {
  final ServiceResponse? item;
  final bool viewOnly;
  final VoidCallback onSuccess;

  const ServiceFormDialog({
    super.key,
    this.item,
    this.viewOnly = false,
    required this.onSuccess,
  });

  static Future<void> show(
    BuildContext context, {
    ServiceResponse? item,
    bool viewOnly = false,
    required VoidCallback onSuccess,
  }) async {
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom,
        ),
        child: ServiceFormDialog(
          item: item,
          viewOnly: viewOnly,
          onSuccess: onSuccess,
        ),
      ),
    );
  }

  @override
  State<ServiceFormDialog> createState() => _ServiceFormDialogState();
}

class _ServiceFormDialogState extends State<ServiceFormDialog> {
  late TextEditingController _nameController;
  late TextEditingController _priceController;
  late TextEditingController _setupPriceController;
  late TextEditingController _maintainPriceController;
  late TextEditingController _shortDescController;
  late TextEditingController _langController;

  int? _selectedServiceTypeId;
  List<ServiceTypeResponse> _serviceTypes = [];
  bool _isLoadingServiceTypes = false;

  String? _nameError;
  bool _isSaving = false;

  bool get isEditMode => widget.item != null && !widget.viewOnly;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(
      text: widget.item?.serviceName ?? '',
    );
    _priceController = TextEditingController(
      text: widget.item?.servicePrice != null
          ? widget.item!.servicePrice!.toStringAsFixed(0)
          : '',
    );
    _setupPriceController = TextEditingController(
      text: widget.item?.setupPrice != null
          ? widget.item!.setupPrice!.toStringAsFixed(0)
          : '',
    );
    _maintainPriceController = TextEditingController(
      text: widget.item?.maintainPrice != null
          ? widget.item!.maintainPrice!.toStringAsFixed(0)
          : '',
    );
    _shortDescController = TextEditingController(
      text: widget.item?.serviceShortDesc ?? '',
    );
    _langController = TextEditingController(
      text: widget.item?.langId != null ? widget.item!.langId.toString() : '',
    );
    _selectedServiceTypeId = widget.item?.serviceTypeId;

    _fetchServiceTypes();
  }

  Future<void> _fetchServiceTypes() async {
    setState(() => _isLoadingServiceTypes = true);
    try {
      final res = await ServiceTypeApi.getList(
        ServiceTypeFilterParams(page: 1, pageSize: 100),
      );
      setState(() {
        _serviceTypes = res.items;
      });
    } catch (_) {
    } finally {
      if (mounted) {
        setState(() => _isLoadingServiceTypes = false);
      }
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _priceController.dispose();
    _setupPriceController.dispose();
    _maintainPriceController.dispose();
    _shortDescController.dispose();
    _langController.dispose();
    super.dispose();
  }

  bool _validate() {
    String? nameErr;
    if (_nameController.text.trim().isEmpty) {
      nameErr = "Vui lòng nhập tên dịch vụ";
    }

    setState(() {
      _nameError = nameErr;
    });

    return nameErr == null;
  }

  Future<void> _handleSubmit() async {
    if (widget.viewOnly || !_validate()) return;

    setState(() => _isSaving = true);

    try {
      final name = _nameController.text.trim();
      final price = double.tryParse(_priceController.text.trim());
      final setupPrice = double.tryParse(_setupPriceController.text.trim());
      final maintainPrice = double.tryParse(
        _maintainPriceController.text.trim(),
      );
      final langId = int.tryParse(_langController.text.trim());
      final shortDesc = _shortDescController.text.trim().isNotEmpty
          ? _shortDescController.text.trim()
          : null;

      if (isEditMode && widget.item != null) {
        await ServiceApi.update(
          widget.item!.serviceId,
          UpdateServiceRequest(
            serviceName: name,
            serviceTypeId: _selectedServiceTypeId,
            servicePrice: price,
            setupPrice: setupPrice,
            maintainPrice: maintainPrice,
            serviceShortDesc: shortDesc,
            langId: langId,
          ),
        );
        if (mounted) {
          AppToast.success(context, "Cập nhật dịch vụ thành công");
        }
      } else {
        await ServiceApi.create(
          CreateServiceRequest(
            serviceName: name,
            serviceTypeId: _selectedServiceTypeId,
            servicePrice: price,
            setupPrice: setupPrice,
            maintainPrice: maintainPrice,
            serviceShortDesc: shortDesc,
            langId: langId,
          ),
        );
        if (mounted) {
          AppToast.success(context, "Tạo dịch vụ mới thành công");
        }
      }

      widget.onSuccess();
      if (mounted) {
        Navigator.of(context).pop();
      }
    } catch (error) {
      if (mounted) {
        final msg = error.toString().replaceAll(RegExp(r'^Exception:\s*'), '');
        AppToast.error(
          context,
          msg.isNotEmpty ? msg : "Thao tác không thành công",
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    String title = "Thêm dịch vụ mới";
    if (widget.viewOnly) {
      title = "Chi tiết dịch vụ";
    } else if (isEditMode) {
      title = "Chỉnh sửa dịch vụ";
    }

    return Container(
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header bar
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close_rounded),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 12),

            // Input 1: Tên dịch vụ
            const Text(
              'Tên dịch vụ *',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            AppTextField(
              controller: _nameController,
              placeholder: 'Nhập tên dịch vụ...',
              hasError: _nameError != null,
              onChanged: (val) {
                if (_nameError != null) _validate();
              },
            ),
            if (_nameError != null) ...[
              const SizedBox(height: 4),
              Text(
                _nameError!,
                style: TextStyle(color: theme.colorScheme.error, fontSize: 12),
              ),
            ],

            const SizedBox(height: 14),

            // Input 2: Loại dịch vụ Dropdown
            const Text(
              'Loại dịch vụ',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            _isLoadingServiceTypes
                ? const LinearProgressIndicator()
                : Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surfaceContainerHighest
                          .withValues(alpha: 0.3),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: theme.colorScheme.outlineVariant.withValues(
                          alpha: 0.4,
                        ),
                      ),
                    ),
                    child: DropdownButtonHideUnderline(
                      child: DropdownButton<int?>(
                        value: _selectedServiceTypeId,
                        isExpanded: true,
                        hint: const Text('Chọn loại dịch vụ'),
                        items: [
                          const DropdownMenuItem<int?>(
                            value: null,
                            child: Text('Chưa phân loại'),
                          ),
                          ..._serviceTypes.map((st) {
                            return DropdownMenuItem<int?>(
                              value: st.serviceTypeId,
                              child: Text(
                                st.serviceTypeName ??
                                    'Loại ${st.serviceTypeId}',
                              ),
                            );
                          }),
                        ],
                        onChanged: widget.viewOnly
                            ? null
                            : (val) =>
                                  setState(() => _selectedServiceTypeId = val),
                      ),
                    ),
                  ),

            const SizedBox(height: 14),

            // Input 3: Đơn giá dịch vụ
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Giá dịch vụ (VNĐ)',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 4),
                      AppTextField(
                        controller: _priceController,
                        placeholder: '0',
                        keyboardType: TextInputType.number,
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Giá cài đặt (VNĐ)',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 4),
                      AppTextField(
                        controller: _setupPriceController,
                        placeholder: '0',
                        keyboardType: TextInputType.number,
                      ),
                    ],
                  ),
                ),
              ],
            ),

            const SizedBox(height: 14),

            // Input 4: Mô tả ngắn
            const Text(
              'Mô tả ngắn',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            AppTextField(
              controller: _shortDescController,
              placeholder: 'Mô tả tóm tắt dịch vụ...',
              maxLength: 500,
            ),

            const SizedBox(height: 24),

            // Buttons Footer
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: _isSaving
                      ? null
                      : () => Navigator.of(context).pop(),
                  child: Text(widget.viewOnly ? 'Đóng' : 'Hủy bỏ'),
                ),
                if (!widget.viewOnly) ...[
                  const SizedBox(width: 12),
                  ElevatedButton(
                    onPressed: _isSaving ? null : _handleSubmit,
                    child: _isSaving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : Text(isEditMode ? 'Cập nhật' : 'Tạo mới'),
                  ),
                ],
              ],
            ),
            const SizedBox(height: 12),
          ],
        ),
      ),
    );
  }
}
