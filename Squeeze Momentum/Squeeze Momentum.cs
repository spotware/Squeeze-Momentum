using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SqueezeMomentum : Indicator
    {
        private BollingerBands _bbands;

        private KeltnerChannels _keltnerChannels;

        private MovingAverage _ma;

        private LinearRegressionForecast _linearRegression;

        private IndicatorDataSeries _deltaSeries;

        [Parameter("Source", Group = "Bollinger Bands")]
        public DataSeries BollingerBandsSource { get; set; }

        [Parameter("Periods", DefaultValue = 20, MinValue = 1, Group = "Bollinger Bands")]
        public int BollingerBandsPeriods { get; set; }

        [Parameter("Multiplier", DefaultValue = 2, MinValue = 0, Group = "Bollinger Bands")]
        public double BollingerBandsMultiplier { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple, Group = "Bollinger Bands")]
        public MovingAverageType BollingerBandsMaType { get; set; }

        [Parameter("MA Periods", DefaultValue = 20, MinValue = 1, Group = "Keltner Channels")]
        public int KeltnerMaPeriods { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple, Group = "Keltner Channels")]
        public MovingAverageType KeltnerMaType { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 1.5, MinValue = 0, Group = "Keltner Channels")]
        public double KeltnerAtrMultiplier { get; set; }

        [Parameter("ATR Periods", DefaultValue = 20, MinValue = 1, Group = "Keltner Channels")]
        public int KeltnerAtrPeriods { get; set; }

        [Parameter("ATR MA Type", DefaultValue = MovingAverageType.Simple, Group = "Keltner Channels")]
        public MovingAverageType KeltnerAtrMaType { get; set; }

        [Parameter("Midline Periods", DefaultValue = 20, MinValue = 1, Group = "Donchian")]
        public int DonchianMidlinePeriods { get; set; }

        [Parameter("Source", Group = "Moving Average")]
        public DataSeries MaSource { get; set; }

        [Parameter("Periods", DefaultValue = 20, MinValue = 1, Group = "Moving Average")]
        public int MaPeriods { get; set; }

        [Parameter("MA Type", DefaultValue = MovingAverageType.Simple, Group = "Moving Average")]
        public MovingAverageType MaType { get; set; }

        [Parameter("Periods", DefaultValue = 20, MinValue = 1, Group = "Linear Regression")]
        public int LinearRegressionPeriods { get; set; }

        [Output("Up Histogram Bars", LineColor = "Blue", PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries UpHistogramBars { get; set; }

        [Output("Down Histogram Bars", LineColor = "Red", PlotType = PlotType.Histogram, Thickness = 3)]
        public IndicatorDataSeries DownHistogramBars { get; set; }

        [Output("Squeeze On Dots", LineColor = "Lime", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries SqueezeOnDots { get; set; }

        [Output("Squeeze Off Dots", LineColor = "DarkRed", PlotType = PlotType.Points, Thickness = 5)]
        public IndicatorDataSeries SqueezeOffDots { get; set; }

        protected override void Initialize()
        {
            _bbands = Indicators.BollingerBands(BollingerBandsSource, BollingerBandsPeriods, BollingerBandsMultiplier, BollingerBandsMaType);

            _keltnerChannels = Indicators.KeltnerChannels(KeltnerMaPeriods, KeltnerMaType, KeltnerAtrPeriods, KeltnerAtrMaType, KeltnerAtrMultiplier);

            _ma = Indicators.MovingAverage(MaSource, MaPeriods, MaType);

            _deltaSeries = CreateDataSeries();

            _linearRegression = Indicators.LinearRegressionForecast(_deltaSeries, LinearRegressionPeriods);
        }

        public override void Calculate(int index)
        {
            var donchianMidline = (Bars.HighPrices.Maximum(DonchianMidlinePeriods) + Bars.LowPrices.Minimum(DonchianMidlinePeriods)) / 2;
            var donchainMidlineAndMaAverage = (donchianMidline + _ma.Result[index]) / 2;

            _deltaSeries[index] = Bars.ClosePrices[index] - donchainMidlineAndMaAverage;

            UpHistogramBars[index] = double.NaN;
            DownHistogramBars[index] = double.NaN;

            var linearRegression = _linearRegression.Result[index];

            if (linearRegression > 0)
            {
                UpHistogramBars[index] = linearRegression;
            }
            else
            {
                DownHistogramBars[index] = linearRegression;
            }

            SqueezeOnDots[index] = double.NaN;
            SqueezeOffDots[index] = double.NaN;

            var isSqueezeOn = _bbands.Top[index] < _keltnerChannels.Top[index] && _bbands.Bottom[index] > _keltnerChannels.Bottom[index];

            if (isSqueezeOn)
            {
                SqueezeOnDots[index] = 0;
            }
            else
            {
                SqueezeOffDots[index] = 0;
            }
        }
    }
}