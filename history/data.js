window.BENCHMARK_DATA = {
  "lastUpdate": 1793039785083,
  "repoUrl": "https://github.com/rent-a-developer/ResXLocalization",
  "entries": {
    "Time": [
      {
        "commit": {
          "author": {
            "email": "info@rent-a-developer.de",
            "name": "David Liebeherr",
            "username": "rent-a-developer"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "0dcf57dbc7137d81233f42b157c1dc175a9b85d3",
          "message": "feat: Add a performance benchmark suite with CI regression checks (#56)\n\n* docs: specify the performance benchmark suite\n\nOpenSpec change add-benchmarks: proposal, design, delta spec and tasks.\n\nRefs: #53\n\n* test: add the BenchmarkDotNet suite and the CI benchmark job\n\nCore, Avalonia and WPF benchmarks against the designer class and\n{x:Static}, a CI job that fails a pull request on allocation regressions,\nthe published history on GitHub Pages, and the benchmarks docs page.\n\nRefs: #53",
          "timestamp": "2026-10-26T19:30:08+01:00",
          "tree_id": "07e28fb692bfaf1d69f9a6fbb9ef400a8e044939",
          "url": "https://github.com/rent-a-developer/ResXLocalization/commit/0dcf57dbc7137d81233f42b157c1dc175a9b85d3"
        },
        "date": 1793039784112,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.Localize",
            "value": 8112460,
            "unit": "ns",
            "range": "± 1740278.6065126797"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.LocalizeEnum",
            "value": 5258460,
            "unit": "ns",
            "range": "± 1074814.8018545865"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.LocalizeWithArguments",
            "value": 4776733.333333333,
            "unit": "ns",
            "range": "± 526256.9115647766"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.Static",
            "value": 3855073.3333333335,
            "unit": "ns",
            "range": "± 394148.89221979957"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenLocalize(ElementCount: 100)",
            "value": 3956640,
            "unit": "ns",
            "range": "± 316091.67657500884"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenStatic(ElementCount: 100)",
            "value": 3688050,
            "unit": "ns",
            "range": "± 295868.14067531266"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenLocalize(ElementCount: 10000)",
            "value": 610980700,
            "unit": "ns",
            "range": "± 17098201.776795126"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenStatic(ElementCount: 10000)",
            "value": 457379420,
            "unit": "ns",
            "range": "± 9448350.635534225"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchLocalize(ElementCount: 100)",
            "value": 7403450,
            "unit": "ns",
            "range": "± 169113.1475275257"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchTranslationSource(ElementCount: 100)",
            "value": 2942450,
            "unit": "ns",
            "range": "± 260659.886953606"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchLocalize(ElementCount: 10000)",
            "value": 1148637750,
            "unit": "ns",
            "range": "± 137511968.29183632"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchTranslationSource(ElementCount: 10000)",
            "value": 378457770,
            "unit": "ns",
            "range": "± 28137172.021136027"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromObject",
            "value": 944.6062914774712,
            "unit": "ns",
            "range": "± 86.95275098268043"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromTuples",
            "value": 819.0070525053828,
            "unit": "ns",
            "range": "± 23.555756631335758"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromValues",
            "value": 571.7220464135021,
            "unit": "ns",
            "range": "± 34.277423255109284"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.StringFormatDesignerProperty",
            "value": 140.05574729373686,
            "unit": "ns",
            "range": "± 0.8572508316513165"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetEnum",
            "value": 114.93129174269048,
            "unit": "ns",
            "range": "± 0.43950064056683075"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetStringKeyScoped",
            "value": 54.007182192917824,
            "unit": "ns",
            "range": "± 0.25455294950831087"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetStringKeySearchAll",
            "value": 55.42338341955194,
            "unit": "ns",
            "range": "± 0.15998715523902726"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetTypedKey",
            "value": 54.54313741774321,
            "unit": "ns",
            "range": "± 0.13907540557283016"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.ResXDesignerProperty",
            "value": 44.13863377648344,
            "unit": "ns",
            "range": "± 1.4637415576731316"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.MessageFormatBenchmarks.GetWithMessageFormat",
            "value": 1816.544473131995,
            "unit": "ns",
            "range": "± 17.841902588117787"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.MessageFormatBenchmarks.MessageFormatterDirect",
            "value": 1096.6219183643343,
            "unit": "ns",
            "range": "± 5.97262286363836"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 1)",
            "value": 55.65629972587348,
            "unit": "ns",
            "range": "± 0.36887578844182456"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 1)",
            "value": 40.906169083141734,
            "unit": "ns",
            "range": "± 0.11437093646445873"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 5)",
            "value": 802.7842274912588,
            "unit": "ns",
            "range": "± 1.6559596725538415"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 5)",
            "value": 772.74434631013,
            "unit": "ns",
            "range": "± 8.15046454294722"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 20)",
            "value": 3590.0972558959666,
            "unit": "ns",
            "range": "± 36.675660366020885"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 20)",
            "value": 3493.741614490161,
            "unit": "ns",
            "range": "± 31.371135104663303"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.Localize",
            "value": 5704420,
            "unit": "ns",
            "range": "± 843986.5200345324"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.LocalizeEnum",
            "value": 3585975,
            "unit": "ns",
            "range": "± 141047.84568623255"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.LocalizeWithArguments",
            "value": 5563091.666666667,
            "unit": "ns",
            "range": "± 164858.07371705206"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.Static",
            "value": 2532707.1428571427,
            "unit": "ns",
            "range": "± 30489.93438663158"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenLocalize(ElementCount: 100)",
            "value": 3216680,
            "unit": "ns",
            "range": "± 282152.3382855439"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenStatic(ElementCount: 100)",
            "value": 3139300,
            "unit": "ns",
            "range": "± 188175.39690405864"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenLocalize(ElementCount: 10000)",
            "value": 426991200,
            "unit": "ns",
            "range": "± 14711325.29736869"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenStatic(ElementCount: 10000)",
            "value": 155085340,
            "unit": "ns",
            "range": "± 9305834.251049178"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchLocalize(ElementCount: 100)",
            "value": 282520,
            "unit": "ns",
            "range": "± 13052.854094028631"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchTranslationSource(ElementCount: 100)",
            "value": 174450,
            "unit": "ns",
            "range": "± 16989.604664813913"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchLocalize(ElementCount: 10000)",
            "value": 17205025,
            "unit": "ns",
            "range": "± 420402.43715595495"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchTranslationSource(ElementCount: 10000)",
            "value": 13485350,
            "unit": "ns",
            "range": "± 197777.07147189736"
          }
        ]
      }
    ],
    "Allocations": [
      {
        "commit": {
          "author": {
            "email": "info@rent-a-developer.de",
            "name": "David Liebeherr",
            "username": "rent-a-developer"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "0dcf57dbc7137d81233f42b157c1dc175a9b85d3",
          "message": "feat: Add a performance benchmark suite with CI regression checks (#56)\n\n* docs: specify the performance benchmark suite\n\nOpenSpec change add-benchmarks: proposal, design, delta spec and tasks.\n\nRefs: #53\n\n* test: add the BenchmarkDotNet suite and the CI benchmark job\n\nCore, Avalonia and WPF benchmarks against the designer class and\n{x:Static}, a CI job that fails a pull request on allocation regressions,\nthe published history on GitHub Pages, and the benchmarks docs page.\n\nRefs: #53",
          "timestamp": "2026-10-26T19:30:08+01:00",
          "tree_id": "07e28fb692bfaf1d69f9a6fbb9ef400a8e044939",
          "url": "https://github.com/rent-a-developer/ResXLocalization/commit/0dcf57dbc7137d81233f42b157c1dc175a9b85d3"
        },
        "date": 1793039785039,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.Localize",
            "value": 2665897,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.LocalizeEnum",
            "value": 2622465,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.LocalizeWithArguments",
            "value": 3431313,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaFormsBenchmarks.Static",
            "value": 2242905,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenLocalize(ElementCount: 100)",
            "value": 2679857,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenStatic(ElementCount: 100)",
            "value": 2256681,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenLocalize(ElementCount: 10000)",
            "value": 251103233,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.OpenStatic(ElementCount: 10000)",
            "value": 208806105,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchLocalize(ElementCount: 100)",
            "value": 2025425,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchTranslationSource(ElementCount: 100)",
            "value": 1358913,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchLocalize(ElementCount: 10000)",
            "value": 187479417,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Avalonia.Benchmarks.AvaloniaScalingBenchmarks.SwitchTranslationSource(ElementCount: 10000)",
            "value": 120839201,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromObject",
            "value": 1513,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromTuples",
            "value": 1393,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.GetWithFromValues",
            "value": 857,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.ArgumentBenchmarks.StringFormatDesignerProperty",
            "value": 105,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetEnum",
            "value": 97,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetStringKeyScoped",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetStringKeySearchAll",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.GetTypedKey",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.CodeLookupBenchmarks.ResXDesignerProperty",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.MessageFormatBenchmarks.GetWithMessageFormat",
            "value": 2540,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.MessageFormatBenchmarks.MessageFormatterDirect",
            "value": 1188,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 1)",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 1)",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 5)",
            "value": 353,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 5)",
            "value": 353,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.GetStringKeySearchAll(ManagerCount: 20)",
            "value": 1673,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.Core.Benchmarks.SearchAllBenchmarks.HandWrittenSearchAll(ManagerCount: 20)",
            "value": 1673,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.Localize",
            "value": 955001,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.LocalizeEnum",
            "value": 955481,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.LocalizeWithArguments",
            "value": 1642985,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfFormsBenchmarks.Static",
            "value": 381049,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenLocalize(ElementCount: 100)",
            "value": 961497,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenStatic(ElementCount: 100)",
            "value": 383337,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenLocalize(ElementCount: 10000)",
            "value": 94685401,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.OpenStatic(ElementCount: 10000)",
            "value": 34888849,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchLocalize(ElementCount: 100)",
            "value": 61817,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchTranslationSource(ElementCount: 100)",
            "value": 11785,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchLocalize(ElementCount: 10000)",
            "value": 5368409,
            "unit": "bytes"
          },
          {
            "name": "RentADeveloper.ResXLocalization.WPF.Benchmarks.WpfScalingBenchmarks.SwitchTranslationSource(ElementCount: 10000)",
            "value": 487145,
            "unit": "bytes"
          }
        ]
      }
    ]
  }
}