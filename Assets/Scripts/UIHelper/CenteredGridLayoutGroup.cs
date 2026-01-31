using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace FrameWork.UI
{
    /// <summary>
    /// Custom Grid Layout Group tự động căn giữa các item khi chúng đứng một mình trên một dòng hoặc cột
    /// </summary>
    [AddComponentMenu("Layout/Centered Grid Layout Group")]
    public class CenteredGridLayoutGroup : GridLayoutGroup
    {
        [Header("Centered Grid Settings")]
        [SerializeField] private bool centerLastRow = true;
        [SerializeField] private bool centerLastColumn = true;
        [SerializeField] private bool centerSingleItem = true;
        [SerializeField] private bool centerGrid = true;

        private List<RectTransform> m_Children = new List<RectTransform>();

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            UpdateChildren();
        }

        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        private void UpdateChildren()
        {
            m_Children.Clear();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                RectTransform child = rectTransform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.ignoreLayout)
                    continue;

                m_Children.Add(child);
            }
        }

        private void SetCellsAlongAxis(int axis)
        {
            if (m_Children.Count == 0)
                return;

            // Tính toán số lượng item trên mỗi dòng/cột
            int cellCountX = 1;
            int cellCountY = 1;

            if (m_Constraint == Constraint.FixedColumnCount)
            {
                cellCountX = m_ConstraintCount;
                if (startAxis == Axis.Horizontal)
                    cellCountY = Mathf.CeilToInt((float)m_Children.Count / cellCountX);
                else
                    cellCountY = Mathf.CeilToInt((float)m_Children.Count / cellCountX);
            }
            else if (m_Constraint == Constraint.FixedRowCount)
            {
                cellCountY = m_ConstraintCount;
                if (startAxis == Axis.Horizontal)
                    cellCountX = Mathf.CeilToInt((float)m_Children.Count / cellCountY);
                else
                    cellCountX = Mathf.CeilToInt((float)m_Children.Count / cellCountY);
            }
            else
            {
                // Flexible: tính toán dựa trên kích thước container
                float width = rectTransform.rect.width;
                float height = rectTransform.rect.height;

                if (startAxis == Axis.Horizontal)
                {
                    int minColumns = 1;
                    int maxColumns = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x) / (cellSize.x + spacing.x)));
                    cellCountX = Mathf.Clamp(m_Children.Count, minColumns, maxColumns);
                    cellCountY = Mathf.CeilToInt((float)m_Children.Count / cellCountX);
                }
                else
                {
                    int minRows = 1;
                    int maxRows = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y) / (cellSize.y + spacing.y)));
                    cellCountY = Mathf.Clamp(m_Children.Count, minRows, maxRows);
                    cellCountX = Mathf.CeilToInt((float)m_Children.Count / cellCountY);
                }
            }

            // Tính toán offset để căn giữa toàn bộ grid
            float totalGridWidth = cellCountX * cellSize.x + (cellCountX - 1) * spacing.x;
            float totalGridHeight = cellCountY * cellSize.y + (cellCountY - 1) * spacing.y;

            float availableWidth = rectTransform.rect.width - padding.horizontal;
            float availableHeight = rectTransform.rect.height - padding.vertical;

            // Offset chung cho toàn bộ grid (căn giữa theo Child Alignment)
            float baseOffsetX = 0;
            float baseOffsetY = 0;

            // Căn giữa theo trục X dựa trên Child Alignment hoặc thuộc tính centerGrid
            if (centerGrid || 
                childAlignment == TextAnchor.UpperCenter ||
                childAlignment == TextAnchor.MiddleCenter ||
                childAlignment == TextAnchor.LowerCenter)
            {
                baseOffsetX = (availableWidth - totalGridWidth) * 0.5f;
            }
            else if (childAlignment == TextAnchor.UpperRight ||
                     childAlignment == TextAnchor.MiddleRight ||
                     childAlignment == TextAnchor.LowerRight)
            {
                baseOffsetX = availableWidth - totalGridWidth;
            }

            // Căn giữa theo trục Y dựa trên Child Alignment hoặc thuộc tính centerGrid
            if (centerGrid || 
                childAlignment == TextAnchor.MiddleLeft ||
                childAlignment == TextAnchor.MiddleCenter ||
                childAlignment == TextAnchor.MiddleRight)
            {
                baseOffsetY = (availableHeight - totalGridHeight) * 0.5f;
            }
            else if (childAlignment == TextAnchor.LowerLeft ||
                     childAlignment == TextAnchor.LowerCenter ||
                     childAlignment == TextAnchor.LowerRight)
            {
                baseOffsetY = availableHeight - totalGridHeight;
            }

            // Đặt vị trí cho từng child
            for (int i = 0; i < m_Children.Count; i++)
            {
                int positionX, positionY;

                // Tính toán vị trí dựa trên Start Axis
                if (startAxis == Axis.Horizontal)
                {
                    positionX = i % cellCountX;
                    positionY = i / cellCountX;
                }
                else // Vertical
                {
                    positionX = i / cellCountY;
                    positionY = i % cellCountY;
                }

                // Tính offset riêng cho dòng/cột cuối cùng nếu có ít item hơn
                float rowOffsetX = 0;
                float columnOffsetY = 0;

                if (startAxis == Axis.Horizontal)
                {
                    // Căn giữa dòng cuối cùng
                    bool isLastRow = positionY == cellCountY - 1;
                    int itemsInCurrentRow = Mathf.Min(cellCountX, m_Children.Count - positionY * cellCountX);

                    if (isLastRow && centerLastRow && itemsInCurrentRow < cellCountX)
                    {
                        float totalRowWidth = itemsInCurrentRow * cellSize.x + (itemsInCurrentRow - 1) * spacing.x;

                        // Căn giữa dòng cuối cùng
                        if (childAlignment == TextAnchor.UpperCenter ||
                            childAlignment == TextAnchor.MiddleCenter ||
                            childAlignment == TextAnchor.LowerCenter)
                        {
                            rowOffsetX = (totalGridWidth - totalRowWidth) * 0.5f;
                        }
                        else if (childAlignment == TextAnchor.UpperRight ||
                                 childAlignment == TextAnchor.MiddleRight ||
                                 childAlignment == TextAnchor.LowerRight)
                        {
                            rowOffsetX = totalGridWidth - totalRowWidth;
                        }
                    }
                }
                else // Vertical
                {
                    // Căn giữa cột cuối cùng
                    bool isLastColumn = positionX == cellCountX - 1;
                    int itemsInCurrentColumn = Mathf.Min(cellCountY, m_Children.Count - positionX * cellCountY);

                    if (isLastColumn && centerLastColumn && itemsInCurrentColumn < cellCountY)
                    {
                        float totalColumnHeight = itemsInCurrentColumn * cellSize.y + (itemsInCurrentColumn - 1) * spacing.y;

                        // Căn giữa cột cuối cùng
                        if (childAlignment == TextAnchor.MiddleLeft ||
                            childAlignment == TextAnchor.MiddleCenter ||
                            childAlignment == TextAnchor.MiddleRight)
                        {
                            columnOffsetY = (totalGridHeight - totalColumnHeight) * 0.5f;
                        }
                        else if (childAlignment == TextAnchor.LowerLeft ||
                                 childAlignment == TextAnchor.LowerCenter ||
                                 childAlignment == TextAnchor.LowerRight)
                        {
                            columnOffsetY = totalGridHeight - totalColumnHeight;
                        }
                    }
                }

                SetChildAlongAxis(m_Children[i], 0,
                    padding.left + baseOffsetX + rowOffsetX + (cellSize.x + spacing.x) * positionX,
                    cellSize.x);
                SetChildAlongAxis(m_Children[i], 1,
                    padding.top + baseOffsetY + columnOffsetY + (cellSize.y + spacing.y) * positionY,
                    cellSize.y);
            }
        }

        /// <summary>
        /// Bật/tắt tính năng căn giữa dòng cuối cùng
        /// </summary>
        public void SetCenterLastRow(bool value)
        {
            if (centerLastRow != value)
            {
                centerLastRow = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Bật/tắt tính năng căn giữa cột cuối cùng
        /// </summary>
        public void SetCenterLastColumn(bool value)
        {
            if (centerLastColumn != value)
            {
                centerLastColumn = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Bật/tắt tính năng căn giữa item đơn lẻ
        /// </summary>
        public void SetCenterSingleItem(bool value)
        {
            if (centerSingleItem != value)
            {
                centerSingleItem = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Bật/tắt tính năng căn giữa toàn bộ khối grid
        /// </summary>
        public void SetCenterGrid(bool value)
        {
            if (centerGrid != value)
            {
                centerGrid = value;
                SetDirty();
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
