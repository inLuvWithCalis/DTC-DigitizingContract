import 'package:flutter/material.dart';
import '../../../models/catalog/service_type_dto.dart';
import '../../../services/catalog/service_types_api.dart';
import '../../../utils/app_toast.dart';
import '../../../widgets/app_text_field.dart';

class ServiceTypeFormDialog extends StatefulWidget {
  final ServiceTypeResponse? item;
  final bool viewOnly;
  final VoidCallback onSuccess;

  const ServiceTypeFormDialog({
    super.key,
    this.item,
    this.viewOnly = false,
    required this.onSuccess,
  });

  static Future<void> show(
    BuildContext context, {
    ServiceTypeResponse? item,
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
        child: ServiceTypeFormDialog(
          item: item,
          viewOnly: viewOnly,
          onSuccess: onSuccess,
        ),
      ),
    );
  }

  @override
  State<ServiceTypeFormDialog> createState() => _ServiceTypeFormDialogState();
}

class _ServiceTypeFormDialogState extends State<ServiceTypeFormDialog> {
  late TextEditingController _nameController;
  late TextEditingController _langController;

  String? _nameError;
  String? _langError;
  bool _isSaving = false;

  bool get isEditMode => widget.item != null && !widget.viewOnly;

  @override
  void initState() {
    super.initState();
    _nameController = TextEditingController(
      text: widget.item?.serviceTypeName ?? '',
    );
    _langController = TextEditingController(
      text: widget.item?.langId != null ? widget.item!.langId.toString() : '',
    );
  }

  @override
  void dispose() {
    _nameController.dispose();
    _langController.dispose();
    super.dispose();
  }

  bool _validate() {
    String? nameErr;
    String? langErr;

    if (_nameController.text.trim().isEmpty) {
      nameErr = "Vui lòng nhập tên loại dịch vụ";
    }

    if (_langController.text.trim().isNotEmpty &&
        int.tryParse(_langController.text.trim()) == null) {
      langErr = "ID Ngôn ngữ phải là số";
    }

    setState(() {
      _nameError = nameErr;
      _langError = langErr;
    });

    return nameErr == null && langErr == null;
  }

  Future<void> _handleSubmit() async {
    if (widget.viewOnly || !_validate()) return;

    setState(() => _isSaving = true);

    try {
      final name = _nameController.text.trim();
      final langIdVal = _langController.text.trim().isNotEmpty
          ? int.parse(_langController.text.trim())
          : null;

      if (isEditMode && widget.item != null) {
        await ServiceTypeApi.update(
          widget.item!.serviceTypeId,
          UpdateServiceTypeRequest(serviceTypeName: name, langId: langIdVal),
        );
        if (mounted) {
          AppToast.success(context, "Cập nhật loại dịch vụ thành công");
        }
      } else {
        await ServiceTypeApi.create(
          CreateServiceTypeRequest(serviceTypeName: name, langId: langIdVal),
        );
        if (mounted) {
          AppToast.success(context, "Thêm loại dịch vụ mới thành công");
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

    String title = "Thêm loại dịch vụ mới";
    if (widget.viewOnly) {
      title = "Chi tiết loại dịch vụ";
    } else if (isEditMode) {
      title = "Chỉnh sửa loại dịch vụ";
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
          // Header title bar
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

          // Input 1: Tên loại dịch vụ
          const Text(
            'Tên loại dịch vụ *',
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 6),
          AppTextField(
            controller: _nameController,
            placeholder: 'Ví dụ: Tư vấn, Triển khai, Hosting...',
            maxLength: 200,
            readOnly: widget.viewOnly,
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

          const SizedBox(height: 16),

          // Input 2: ID Ngôn ngữ
          const Text(
            'ID Ngôn ngữ',
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 6),
          AppTextField(
            controller: _langController,
            placeholder: 'Ví dụ: 1 (VN)',
            readOnly: widget.viewOnly,
            hasError: _langError != null,
            onChanged: (val) {
              if (_langError != null) _validate();
            },
          ),
          if (_langError != null) ...[
            const SizedBox(height: 4),
            Text(
              _langError!,
              style: TextStyle(color: theme.colorScheme.error, fontSize: 12),
            ),
          ],

          if (widget.viewOnly && widget.item != null) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest.withValues(
                  alpha: 0.3,
                ),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Dịch vụ đang sử dụng:'),
                  Text(
                    '${widget.item!.serviceCount} dịch vụ',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ],

          const SizedBox(height: 24),

          // Buttons Footer
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: _isSaving ? null : () => Navigator.of(context).pop(),
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
