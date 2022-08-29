■使用法

次のファイルを同じディレクトリに置き、srell.hppをincludeするだけです。
・srell.hpp
・srell_ucfdata2.hpp（case folding用データ）
・srell_updata.hpp（Unicode property用データ）

■付属物
以下のディレクトリ内にあるものはおまけのようなものです。
SRELL側からは参照していませんので、削除してしまってもライブラリの動作に
影響はありません。

・misc
  簡単なテスト及びベンチマークプログラムのソースが入っています。

・single-header
  srell.hppの中にsrell_ucfdata2.hppとsrell_updata.hppとを統合してしまい、
  これ単体で使用できるようにしたstandalone版が入っています。

・unicode
  最新のUnicodeデータからsrell_ucfdata.hpp及びsrell_updata.hppを作るため
  のプログラムのソースが入っています。

