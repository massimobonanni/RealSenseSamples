using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FaceExpressionWPF
{
    public class FaceModel : INotifyPropertyChanged
    {

        private class ExpressionSelector
        {
            public PXCMFaceData.ExpressionsData.FaceExpression Expression { get; set; }
            public int Threshold { get; set; }
            public Action DetectedExpressionAction { get; set; }
            public Action NotDetectedExpressionAction { get; set; }

            public void CheckExpression(PXCMFaceData.ExpressionsData expressionData)
            {
                if (expressionData == null) return;
                PXCMFaceData.ExpressionsData.FaceExpressionResult exprResult;
                if (expressionData.QueryExpression(Expression, out exprResult))
                {
                    if (exprResult.intensity >= Threshold)
                    {
                        if (DetectedExpressionAction != null)
                            DetectedExpressionAction.Invoke();
                    }
                    else
                    {
                        if (NotDetectedExpressionAction != null)
                            NotDetectedExpressionAction.Invoke();
                    }
                }
                else
                {
                    if (NotDetectedExpressionAction != null)
                        NotDetectedExpressionAction.Invoke();
                }
            }
        }

        public FaceModel()
        {
            Expressions = new List<ExpressionSelector>();
            Expressions.Add(new ExpressionSelector()
            {
                Expression = PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT,
                Threshold = 80,
                DetectedExpressionAction = () => IsEyeRightClosed = true,
                NotDetectedExpressionAction = () => IsEyeRightClosed = false
            });
            Expressions.Add(new ExpressionSelector()
            {
                Expression = PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT,
                Threshold = 80,
                DetectedExpressionAction = () => IsEyeLeftClosed = true,
                NotDetectedExpressionAction = () => IsEyeLeftClosed = false
            });
            Expressions.Add(new ExpressionSelector()
            {
                Expression = PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT ,
                Threshold = 100,
                DetectedExpressionAction = () => IsTongueOut = true,
                NotDetectedExpressionAction = () => IsTongueOut = false
            });
            //Expressions.Add(new ExpressionSelector()
            //{
            //    Expression = PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN,
            //    Threshold = 0,
            //    DetectedExpressionAction = () =>
            //    {
            //        IsMouthNormal = false;
            //        IsSmiling = false;
            //        HasMouthOpen = true;
            //    },
            //    NotDetectedExpressionAction = () =>
            //    {
            //        HasMouthOpen = false;
            //        IsMouthNormal = true;
            //    }
            //});
            //Expressions.Add(new ExpressionSelector()
            //{
            //    Expression = PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE,
            //    Threshold = 50,
            //    DetectedExpressionAction = () =>
            //    {
            //        IsMouthNormal = false;
            //        IsSmiling = true;
            //        HasMouthOpen = false;
            //    },
            //    NotDetectedExpressionAction = () =>
            //    {
            //        IsSmiling = false;
            //        IsMouthNormal = true;
            //    }
            //});
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private List<ExpressionSelector> Expressions;

        private bool _isFaceVisible = false;
        public bool IsFaceVisible
        {
            get { return _isFaceVisible; }
            set
            {
                _isFaceVisible = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isEyeRightClosed = false;
        public bool IsEyeRightClosed
        {
            get { return _isEyeRightClosed; }
            set
            {
                _isEyeRightClosed = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isEyeLeftClosed = false;
        public bool IsEyeLeftClosed
        {
            get { return _isEyeLeftClosed; }
            set
            {
                _isEyeLeftClosed = value;
                NotifyPropertyChanged();
            }
        }

        private bool _hasMouthOpen = false;
        public bool HasMouthOpen
        {
            get { return _hasMouthOpen; }
            set
            {
                _hasMouthOpen = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isSmiling = false;
        public bool IsSmiling
        {
            get { return _isSmiling; }
            set
            {
                _isSmiling = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isMouthNormal = true;
        public bool IsMouthNormal
        {
            get { return _isMouthNormal; }
            set
            {
                _isMouthNormal = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isTongueOut = false;
        public bool IsTongueOut
        {
            get { return _isTongueOut; }
            set
            {
                _isTongueOut = value;
                NotifyPropertyChanged();
            }
        }

        public void SetExpressionData(PXCMFaceData.ExpressionsData expressionData)
        {
            if (expressionData != null)
            {
                IsFaceVisible = true;
                foreach (var expression in Expressions)
                {
                    expression.CheckExpression(expressionData);
                }
            }
            else
                IsFaceVisible = false;
        }

    }
}