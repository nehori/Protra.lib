// Copyright (C) 2008, 2013 panacoran <panacoran@users.sourceforge.jp>
// Copyright (C) 2011 Daisuke Arai <darai@users.sourceforge.jp>
// 
// This program is part of Protra.
//
// Protra is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, see <http://www.gnu.org/licenses/>.
// 
// $Id: SimulateBuiltins.cs 481 2013-07-08 03:32:30Z panacoran $

using Protra.Lib.Config;
using Protra.Lib.Data;

namespace Protra.Lib.Lang.Builtins
{
    /// <summary>
    /// テキストを出力するデリゲート
    /// </summary>
    /// <param name="text">出力するテキスト</param>
    public delegate void AppendTextDelegate(string text);

    /// <summary>
    /// シミュレーション関連の組み込み関数を処理するクラス。
    /// </summary>
    public class SimulateBuiltins : BasicBuiltins
    {
        /// <summary>
        /// システムのファイル名を取得または設定する。
        /// </summary>
        public string System { get; set; }

        /// <summary>
        /// 銘柄リストを取得または設定する。
        /// </summary>
        public BrandList BrandList { get; set; }

        /// <summary>
        /// LogDataのインスタンスを取得または設定する。
        /// </summary>
        public LogData LogData { get; set; }

        /// <summary>
        /// テキストを出力するデリゲートを取得または設定する。
        /// </summary>
        public AppendTextDelegate AppendText { get; set; }

        /// <summary>
        /// 組み込み関数を実行する。
        /// </summary>
        /// <param name="name">名前</param>
        /// <param name="args">引数</param>
        /// <param name="at">int型@作用素</param>
        /// <param name="ats">string型@作用素</param>
        /// <returns></returns>
        public override Value Invoke(string name, Value[] args, int at, string ats)
        {
            if (args.Length == 0)
            {
                switch (name)
                {
                    case "CodeList":
                        var codeList = new Value[BrandList.List.Count];
                        for (var i = 0; i < codeList.Length; i++)
                            codeList[i] = new Value(BrandList.List[i]);
                        return new Value(codeList);
                    default:
                        return base.Invoke(name, args, at, ats);
                }
            }
            var brand = Brand;
            if (ats != null)
            {
                if (!GlobalEnv.BrandData.Contains(ats))
                    throw new RuntimeException("missing brand code " + ats, null);
                brand = GlobalEnv.BrandData[ats];
            }

            string msg;
            if (args.Length == 1)
            {
                var prefix = "";
                switch (name)
                {
                    case "Print":
                        break;
                    case "PrintLog":
                        prefix = string.Format(
                            "{0} {1} {2} ",
                            brand.Code, brand.Name,
                            Prices[Index + at].Date.ToString("yy/MM/dd"));
                        break;
                    default:
                        return base.Invoke(name, args, at, ats);
                }
                if (args[0] == null)
                    msg = "null";
                else if (args[0].ValueType == Value.Type.Array)
                    throw new RuntimeException("wrong type argument for " + name + "(1)", null);
                else
                    msg = args[0].InnerValue.ToString();
                AppendText(prefix + msg + "\r\n");
                return null;
            }
            if (args.Length != 2)
                return base.Invoke(name, args, at, ats);
            var log = new Log
                {
                    Date = Prices[Index + at].Date,
                    Code = brand.Code,
                    Price = (int)args[0].InnerValue,
                    Quantity = (int)args[1].InnerValue
                };
            msg = string.Format("{0} {1} {2} {3}円 {4}株 ",
                                brand.Code, brand.Name,
                                log.Date.ToString("yy/MM/dd"),
                                log.Price, log.Quantity);
            switch (name)
            {
                case "Buy":
                    log.Order = Order.Buy;
                    LogData.Add(log);
//                    if (!LogData.Add(log))
//                        throw new RuntimeException("同日の売買があります。", null);
                    msg += "買\r\n";
                    break;
                case "Sell":
                    log.Order = Order.Sell;
                    LogData.Add(log);
//                    if (!LogData.Add(log))
//                        throw new RuntimeException("同日の売買があります。", null);
                    msg += "売\r\n";
                    break;
                default:
                    return base.Invoke(name, args, at, ats);
            }
            AppendText(msg);
            return null;
        }
    }
}