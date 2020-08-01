# Message Board Sample

このプロジェクトは、ASP.NET と SQL Server を使って実行できる、簡単なメッセージ ボードのサンプル プログラムです。Windows 環境の Visual Studio 2019 を用いて .NET Framework 4.8 をターゲットした状態でビルドを行うと、IIS Express 上でアプリケーションが開始され、SQL Server LocalDB 上に Code-first で Entity Framework によるモデルが作成され、Web ページを見ることができます。

このプロジェクトの目的として、Windows ノード プール環境を有効にした Azure Kubernetes Service 環境にデプロイを行い、マイクロ サービスとして運用し、Azure DevOps によって CI/CD を有効化する手法を解説することがあります。詳細は [これから始めるAzure Kubernetes Service入門](https://www.slideshare.net/YutoTakei/azure-kubernetes-service-237315668) をご覧ください。

## プロジェクトの構成

次の箇所を参照することで、プロジェクトの全体像を理解できます。プロジェクトは、ASP.NET アプリケーションとして稼働するための、その他の最低限のファイルを含みます。

* アプリケーションの機能に関するファイル
    * [Controllers/HomeController.cs](Controllers/HomeController.cs) ... ページがレンダリングされる際の処理を定義しています。
    * [Models/MessageBoardModel.cs](Models/MessageBoardModel.cs) ... データベースに保存するメッセージのモデルを定義しています。
    * [Views/Home/Index.cshtml](Views/Home/Index.cshtml) ... メッセージ ボードの画面ビューを定義しています。
* コンテナ化に関するファイル群
    * [chart/](chart) ... このアプリケーションを Kubernetes クラスタに展開するための Helm チャートの定義が入っています。
    * [Dockerfile](Dockerfile) ... このアプリケーションを Windows コンテナとしてビルドする方法を定義しています。
* Azure に関する部分
    * [az.azcli](az.azcli) ... Azure 上に必要なリソースを定義するための Azure CLI のコマンド ラインが記述されています。
    * [azure-pipelines.yml](azure-pipelines.yml) ... このアプリケーションに対して CI/CD を構築するためのビルド パイプラインの定義が記述されています。

## プロジェクトのライセンス

[LICENSE](LICENSE) をご覧ください。

## 必要なツールについて

* [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) ... プロジェクトを開く場合
* [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli-windows)
* [Docker Desktop](https://www.docker.com/products/docker-desktop)
* [Helm](https://helm.sh/docs/intro/install/)

# Azure Kubernetes Service のデプロイ方法

デモ目的で、本アプリケーションを Azure Kubernetes Service 上に展開することを考えます。[az.azcli](az.azcli) ファイル中に、Azure CLI を用いて実行すべきコマンドについて、コメント付きで解説をしています。次の手順を取ります。

1. リソース グループを作成する。
2. このプロジェクトをビルドしたコンテナを格納するために、Azure Container Registry を作成する。
3. Azure Kubernetes Service のクラスタを作成する。
4. Windows ノード プールを追加する。
5. ローカル環境から kubectl を通してクラスタを管理できるようにする。
6. Kubernetes ダッシュボードを表示する。

参照: [Azure での Kubernetes 入門](https://docs.microsoft.com/learn/paths/intro-to-kubernetes-on-azure/)

## ローカルでコンテナをビルドする方法

Docker Desktop を、Windows コンテナを実行できるモードに変更します。その上で、次のコマンドを実行します。

```
docker build --rm --tag=msgboard .
```

これにより、ビルドのための .NET Framework SDK がインストールされたコンテナ (通常 3GB 程度) がダウンロードされ、ローカル環境でコンテナを構築することができます。

## Helm チャートについて

[chart/](chart) ディレクトリには、このプロジェクトを展開できるようにした Helm チャートの定義があります。Kubertes クラスタを作成した後、クラスタ上に名前空間を作成して、アプリケーションを展開するには、以下のコマンドを実行します。

```
kubectl create ns msg-board
helm install msgboard chart/ --namespace=msg-board
```

また、実行したアプリケーションを完全に削除し、名前空間を消去するには、以下のようにします。

```
helm uninstall --namespace=msg-board msgboard
kubectl delete ns msg-board
```

参照: [Azure Kubernetes Service (AKS) での Helm を使用した既存のアプリケーションのインストール](https://docs.microsoft.com/azure/aks/kubernetes-helm)

## チャートが展開するアプリケーションについて

**[チャートの中身を理解したい人向け]** この Helm チャートは、おもに次の 3 つのデプロイメントを作成することができます。

* `msgboard` ... 本アプリケーションの本体です。
* `mssql` ... Linux 版の Microsoft SQL Server を実行します。これは、ローカルで実行していた際の LocalDB の役割を果たします。
* `nginx` ... リバース プロキシの役割を果たします。デフォルトでは、デプロイされません。

また、依存関係として、次の外部チャートを利用しています。

* `ingress-nginx` ... Kubernetes のイングレス コントローラー機能を利用するために展開します。

### サービスの公開方法

本アプリケーションは、サービスの公開方法について、次の 3 種類のデプロイ方法があります。詳細は chart/values.yaml を参照してください。

1. **(既定)** イングレス コントローラーを使う方法。 `ingress.enabled` を `true` に設定します。この場合、アプリケーションと同じ名前空間にデプロイされる NGINX Ingress Controller を用いて、サービスが公開されます。
2. `nginx` デプロイメントを使う方法。 `nginx.create` を `true` に設定します。この場合、`service.type` で指定した方法によって、Nginx がリバース プロキシとして機能してアプリケーションを公開します。
3. 上記のどちらも利用しない方法。`ingress.enabled` および `nginx.create` をともに `false` に設定します。この場合、`service.type` で指定した方法によって、ASP.NET のアプリケーションが直接公開されます。

# Azure DevOps で CI/CD を構築する方法

このデモ プロジェクトは、Azure DevOps を使ってコンテナのビルドを行い、Azure Kubernetes Service へ継続的デプロイ (CI/CD) を行うを含んでいます。

任意の名称で新規に Azure DevOps のプロジェクトを作成したら、以下の手順に沿ってビルド パイプラインとリリース パイプラインを構成してください。

## サービス接続の作成

それぞれのパイプラインが動作するために、次の 3 のサービス接続が必要になります。

1. デモでは、GitHub のレポジトリから、ソースコードの継続的デプロイメントを実行します。したがって、GitHub アカウントへの OAuth 接続が必要になります。
2. Azure Kubernetes Service (AKS) への接続のために、Azure Resource Manager へのサービス接続が必要となります。これは、後続のリリース パイプラインの作成時に必要になります。
3. ビルドパイプラインの中で、acr というサービス接続名で Docker Registry を指定しています。したがって、プロジェクトの設定から、サービス接続を開き、あらかじめ acr という名称の Docker Registry 接続を作っておく必要があります。

参照: [Service connections](https://docs.microsoft.com/azure/devops/pipelines/library/service-endpoints) (現時点で英語のみ)

## ビルド パイプラインの作成

ビルド パイプラインは YAML 形式で記述されているので、非常に容易にパイプラインが作成できます。 [azure-pipelines.yml](azure-pipelines.yml) に定義があります。

1. コードの保存元として GitHub を選択し、このソース コードをフォークした自身のレポジトリを選択します。必要に応じて OAuth での認証をするように求められる場合があります。
2. パイプラインの YAML の記述を確認し、右上の Run または Save で保存します。

参照

* [Azure Pipelines を使用して継続的デリバリーを構成する](https://docs.microsoft.com/azure/service-fabric/service-fabric-tutorial-deploy-app-with-cicd-vsts#configure-continuous-delivery-with-azure-pipelines)
* [チュートリアル:Azure Pipelines を使用した Azure Resource Manager テンプレートの継続的インテグレーション](https://docs.microsoft.com/azure/azure-resource-manager/templates/deployment-tutorial-pipeline)

## リリース パイプラインの構成方法

リリース パイプラインの YAML 形式での記述機能は、2020年7月現在まだ実装されていませんので、GUI で作成していきます。

### 独自の証明書を設定する場合

先に左側メニューの Pipelines の中から、Library を開き、Secure files として、TLS に用いる証明書 (crt ファイル) と秘密鍵 (key ファイル) をアップロードします。拡張子は何でも構わないです。ただしどちらもテキスト形式 (`-----BEGIN CERTIFICATE-----` などで始まるもの) である必要があります。

なお、証明書を設定しない場合、Kubernetes によって Fake Certificate (いわゆるオレオレ証明書) が自動的に作成されます。

参照: [Secure files](https://docs.microsoft.com/azure/devops/pipelines/library/secure-files) (現時点で英語のみ)

### パイプラインの作成

左側メニューの Pipelines の中から、Releases を開き、新規パイプラインの作成を選択します。このとき、テンプレートは選択せずに、空のジョブ (Empty job) からパイプラインを作成します。

アーティファクト (Artifacts) の設定
1. アーティファクトの設定には、ビルド パイプラインの最新のバージョンから生成されるものを指定します。
2. アーティファクトのエイリアスは `msgboard-chart` とします。ここで決定する名前は、後にアーティファクトがダウンロードされるディレクトリ名に影響します。
3. 継続デプロイメントのトリガーを有効にし、ブランチ フィルタは `master` とします。

ステージ (Stages) の設定
1. デプロイメントの前条件として、承認を必要とするように変更をします。このときテストのため、承認者に自分のユーザー アカウントを指定します。なお、個人の通知設定で、リリース時の承認の際に通知を行う設定にしていない場合、メール等は届きません。
2. 続いて [Stage 1] の設定を行うために 0 job, 0 task のリンクを開きます。
3. デフォルトの Agent job を選択し、右の Agent pool が Azure Pipelines (デフォルトのもの) になっていることを確認し、Agent Specification に `ubuntu-20.04` を選択します。ここで、Linux 系を選択しない場合、helm への引数としてパスを渡す場合、バックス ラッシュなどがうまく渡せない事象が発生します。
4. 以下のようなタスクを追加していきます。
    1. (TLS を構成する場合) Download secure file タスクを 2 つ追加し、先に作った TLS の証明書と秘密鍵の Secure file をそれぞれダウンロードするように設定します。このとき、Output Variables を開き、Reference name をそれぞれ `tlscrt` および `tlskey` とします。この名称は後ほど、helm にパスを渡す引数で用います。
    2. Install Helm タスクを追加します。バージョンは最新のものを用いるべきです。このドキュメント作成時点では `3.2.4` です。Prerequisite から追加でインストール設定できる Kubectl も同様で、現時点での最新は `1.18.6` です。なお Kubectl の最新バージョンは https://storage.googleapis.com/kubernetes-release/release/stable.txt から判断できます。
    3. Kubectl タスクを追加し、Azure Resource Manager のサービス接続を用いて、対象の Kubernetes を適切に指定します。コマンドには `create`、引数には `namespace msg-board` を指定します。下の Control Options で Conitnue on error は選択しておきます。これは先の名前空間がすでに存在していてもエラー終了にしないようにするためです。
    4. Helm タスクを追加し、先程と同様に適切な Kubernetes クラスタを選択し、名前空間を `msg-board` とします。コマンドには `upgrade`、チャートの指定は (名前ではなく) ファイル パスとして、そのチャート パスには
        ```
        $(System.DefaultWorkingDirectory)/msgboard-chart/drop
        ```
        を指定します。なお、先程のアーティファクト名が、このパスに影響します。続いてリリース名は `msgboard` とし、もしリリースが存在しない場合にインストールをする、にチェックをします。Wait オプションのチェックは外します。そして、独自の証明書を使用する場合、次の引数を指定します。
        ```
        --set msgboard.image.repository={ACR の名称}.azurecr.io/master --set msgboard.image.pullPolicy=Always --set ingress.host={ドメイン名} --set-file ingress.tls.key=$(tlskey.secureFilePath) --set-file ingress.tls.certificate=$(tlscrt.secureFilePath)
        ```
        なお、ACR の名称とドメイン名は、自身の環境に合わせて適切に変更してください。
5. ステージの設定は以上なので、適切なパイプラインの名称を設定して保存します。
