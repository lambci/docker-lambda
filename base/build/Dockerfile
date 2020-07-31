FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin \
  PIPX_BIN_DIR=/usr/local/bin \
  PIPX_HOME=/usr/local/pipx

RUN chown root:root /tmp && \
  chmod 1777 /tmp && \
  yum groups mark convert && \
  yum groupinstall -y development && \
  yum install -y clang cmake docker python27-devel python36-devel \
    ImageMagick-devel-6.7.8.9 cairo-devel libssh2-devel libxslt-devel libmpc-devel readline-devel db4-devel \
    libffi-devel expat-devel libicu-devel lua-devel gdbm-devel sqlite-devel pcre-devel libcurl-devel && \
  yum clean all && \
  alternatives --set gcc /usr/bin/gcc48 && \
  alternatives --set g++ /usr/bin/g++48 && \
  alternatives --set cpp /usr/bin/cpp48 && \
  python3 -m pip install -U pip setuptools wheel --no-cache-dir && \
  pip install pipx --no-cache-dir && \
  pipx run awscli==1.* 2>/dev/null || true
