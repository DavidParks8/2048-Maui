# Dockerfile for .NET MAUI Android development
FROM mcr.microsoft.com/devcontainers/base:bookworm

# Bootstrap Android command-line tools; post-create installs the required JDK.
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        openjdk-17-jdk \
        unzip \
        wget \
        curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV PATH="${JAVA_HOME}/bin:${PATH}"

# Android SDK setup
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=/opt/android-sdk
ENV PATH="${PATH}:${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools"

# Install Android SDK
RUN mkdir -p ${ANDROID_HOME} \
    && cd /tmp \
    && wget -q https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip -O cmdline-tools.zip \
    && unzip -q cmdline-tools.zip \
    && mkdir -p ${ANDROID_HOME}/cmdline-tools \
    && mv cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
    && rm cmdline-tools.zip \
    && yes | ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager --licenses > /dev/null 2>&1 \
    && ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager --install \
        "platform-tools" \
    && chown -R vscode:vscode ${ANDROID_HOME}

# Switch to vscode user for .NET operations
USER vscode

# Install .NET 11 SDK
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 11.0.100 --install-dir /home/vscode/.dotnet

ENV DOTNET_ROOT=/home/vscode/.dotnet
ENV PATH="${DOTNET_ROOT}:${DOTNET_ROOT}/tools:${PATH}"

# Install MAUI Android workload
RUN /home/vscode/.dotnet/dotnet workload install maui-android --version 11.0.100.1 --skip-sign-check
