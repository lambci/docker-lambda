FROM lambci/lambda-base

ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/opt/bin

# A couple of packages are either missing critical-ish files, or didn't make it into the tar
RUN chmod 1777 /tmp && \
  rpm --rebuilddb && \
  yum reinstall -y setup pam shadow-utils audit-libs openssl glibc-common glibc && \
  yum groupinstall -y development && \
  yum install -y clang cmake docker python27-devel python36-devel \
    ImageMagick-devel-6.7.8.9 cairo-devel libssh2-devel libxslt-devel libmpc-devel readline-devel db4-devel \
    libffi-devel expat-devel libicu-devel lua-devel gdbm-devel sqlite-devel pcre-devel libcurl-devel && \
  alternatives --set gcc /usr/bin/gcc48 && \
  alternatives --set g++ /usr/bin/g++48 && \
  alternatives --set cpp /usr/bin/cpp48 && \
  curl --silent --show-error --retry 5 https://bootstrap.pypa.io/get-pip.py | python3 && \
  pip install -U setuptools wheel --no-cache-dir && \
  yum clean all && \
  rm -rf /var/cache/yum
