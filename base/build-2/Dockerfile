FROM amazonlinux:2

RUN yum list yum && \
  yum install -y --releasever=2 --installroot=/installroot yum yum-plugin-ovl yum-plugin-priorities


FROM lambci/lambda-base-2

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

COPY --from=0 /installroot/etc /etc/
COPY --from=0 /installroot/usr /usr/

RUN yum install -y glibc-langpack-en && \
  yum groupinstall -y development && \
  yum install -y which clang cmake python-devel python3-devel amazon-linux-extras && \
  amazon-linux-extras install -y docker && \
  pip3 install -U pip setuptools --no-cache-dir && \
  yum clean all && \
  rm -rf /var/cache/yum
