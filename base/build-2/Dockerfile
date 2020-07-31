FROM lambci/lambda-base-2

FROM amazonlinux:2

COPY --from=0 / /opt/

RUN yum --installroot=/opt install -y yum yum-plugin-ovl yum-plugin-priorities

FROM lambci/lambda-base-2

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin \
  PIPX_BIN_DIR=/usr/local/bin \
  PIPX_HOME=/usr/local/pipx

COPY --from=1 /opt /

RUN chown root:root /tmp && \
  chmod 1777 /tmp && \
  yum install -y glibc-langpack-en && \
  yum groupinstall -y development && \
  yum install -y which clang cmake python-devel python3-devel amazon-linux-extras && \
  amazon-linux-extras install -y docker && \
  yum clean all && \
  pip3 install -U pip setuptools wheel --no-cache-dir && \
  pip3 install pipx --no-cache-dir
