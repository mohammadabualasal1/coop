/// Fields per API reference §8 — fully documented, no guessing needed.
/// The endpoint returns "the active category tree," but the shape isn't
/// specified as nested; each category carries [parentCategoryId], so the
/// tree is built client-side (see `buildCategoryTree`) regardless of
/// whether the server also nests it.
class CategoryModel {
  const CategoryModel({
    required this.id,
    required this.parentCategoryId,
    required this.nameEn,
    required this.nameAr,
    required this.description,
    required this.imageUrl,
    required this.displayOrder,
    required this.isActive,
  });

  final String id;
  final String? parentCategoryId;
  final String nameEn;
  final String nameAr;
  final String? description;
  final String? imageUrl;
  final int displayOrder;
  final bool isActive;

  factory CategoryModel.fromJson(Map<String, dynamic> json) {
    return CategoryModel(
      id: json['id'] as String,
      parentCategoryId: json['parentCategoryId'] as String?,
      nameEn: json['nameEn'] as String,
      nameAr: json['nameAr'] as String,
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      displayOrder: json['displayOrder'] as int? ?? 0,
      isActive: json['isActive'] as bool? ?? true,
    );
  }
}

class CategoryTreeNode {
  const CategoryTreeNode({required this.category, required this.children});

  final CategoryModel category;
  final List<CategoryTreeNode> children;
}

/// Groups a flat category list into top-level nodes with nested children,
/// ordered by [CategoryModel.displayOrder] at every level.
List<CategoryTreeNode> buildCategoryTree(List<CategoryModel> categories) {
  final byParent = <String?, List<CategoryModel>>{};
  for (final category in categories) {
    byParent.putIfAbsent(category.parentCategoryId, () => []).add(category);
  }
  for (final group in byParent.values) {
    group.sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  List<CategoryTreeNode> build(String? parentId) {
    final children = byParent[parentId] ?? const [];
    return [
      for (final category in children)
        CategoryTreeNode(category: category, children: build(category.id)),
    ];
  }

  return build(null);
}
